using Domain;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Queue
    {
        private readonly Queue<QueuedTaskDto> _pendingTasks = new Queue<QueuedTaskDto>();
        private readonly Dictionary<Guid, QueuedTaskDto> _runningTasks = new Dictionary<Guid, QueuedTaskDto>();
        private readonly Dictionary<Guid, QueuedTaskDto> _tasks = new Dictionary<Guid, QueuedTaskDto>();
        private readonly object _lock = new object();
        private readonly Dojo _dojo;
        private readonly IDojoDb _database;
        private int _maxParallelTasks;
        private HashSet<string> _ninjas = new HashSet<string>();
        private ulong _orderIncrement;

        public string Name => Dto.Name;

        public QueueDto Dto { get; } = new QueueDto();

        public Queue(QueueDto dto, Dojo dojo, IDojoDb database)
        {
            _dojo = dojo;
            _database = database;
            Dto.Name = dto.Name;
            Update(dto);

            var tasks = _database.FetchTasks() ?? Enumerable.Empty<QueuedTaskDto>();
            RestoreTasks(tasks.Where(p => p.QueueName == Name));
        }

        public IEnumerable<QueuedTaskDto> GetAllTasks()
        {
            lock (_lock)
                return _tasks.Values.ToList();
        }

        private void RestoreTasks(IEnumerable<QueuedTaskDto> tasks)
        {
            var pendingTasks = new List<QueuedTaskDto>();
            foreach (var task in tasks)
            {
                _tasks[task.Id] = task;

                switch (task.Status)
                {
                    case QueuedTaskStatus.Pending:
                        pendingTasks.Add(task);
                        break;
                    case QueuedTaskStatus.Running:
                    case QueuedTaskStatus.CancelRequested:
                    case QueuedTaskStatus.CancelPending:
                        _runningTasks[task.Id] = task;
                        break;   
                }
            }

            foreach (var task in pendingTasks.OrderBy(p => p.Order))
                _pendingTasks.Enqueue(task);
        }

        public void Update(QueueDto dto)
        {
            lock (_lock)
            {
                Dto.MaxParallelTasks = dto.MaxParallelTasks;
                Dto.Ninjas = new List<string>(dto.Ninjas ?? Enumerable.Empty<string>());

                _ninjas = new HashSet<string>(dto.Ninjas ?? Enumerable.Empty<string>());

                var current = _maxParallelTasks;
                _maxParallelTasks = dto.MaxParallelTasks;

                var nbTasksToStart = dto.MaxParallelTasks <= 0
                    ? _pendingTasks.Count
                    : dto.MaxParallelTasks - current;
                DequeueTasks(nbTasksToStart);
            }
        }

        public Guid StartTask(string name, StartTaskDto startTask)
        {
            lock (_lock)
            {
                return StartTask(new QueuedTaskDto(Name, name, startTask, _orderIncrement++));
            }
        }

        private Guid StartTask(QueuedTaskDto task)
        {
            // Make sure we didn't exceed the max number of parallel tasks
            if (_maxParallelTasks > 0 && _runningTasks.Count >= _maxParallelTasks)
                return Hang(task);

            var ninjas = new HashSet<string>(_ninjas);
            Ninja ninja;
            while ((ninja = _dojo.GetNextNinja(ninjas)) != null)
            {
                var ninjaId = ninja.StartTask(task);
                // If a task has been launched we record it
                if (ninjaId != Guid.Empty)
                    return RunTask(task, ninja, ninjaId);

                // Otherwise retry with another ninja by banning the faulted one
                ninjas.Remove(ninja.Address);

                // If there is no more usable ninjas we stop retries.
                if (ninjas.Count == 0)
                    break;
            }

            // The task is enqueued with a pending state
            return Hang(task);
        }
        
        private Guid RunTask(QueuedTaskDto task, Ninja ninja, Guid ninjaTaskId)
        {
            task.NinjaState.Id = ninjaTaskId;
            task.NinjaAddress = ninja.Address;

            _runningTasks[task.Id] = task;
            RecordTask(task, QueuedTaskStatus.Running);
            return task.Id;
        }

        private Guid Hang(QueuedTaskDto task)
        {
            if (!_tasks.ContainsKey(task.Id))
                _pendingTasks.Enqueue(task);

            RecordTask(task, QueuedTaskStatus.Pending);
            return task.Id;
        }

        private void RecordTask(QueuedTaskDto task, QueuedTaskStatus status)
        {
            task.Status = status;

            if (!_tasks.ContainsKey(task.Id))
                _database.CreateTask(task);
            else
                _database.UpdateTask(task);

            _tasks[task.Id] = task;
        }

        public void CancelTask(Guid id)
        {
            lock (_lock)
            {
                // Try cancel running task
                if (_runningTasks.TryGetValue(id, out var task))
                {

                    var ninja = _dojo.GetNinja(task.NinjaAddress);
                    if (ninja != null)
                    {
                        if (task.NinjaState == null)
                        {
                            // Todo: The Ninja didn't give any status yet, We can only remove it
                            // Todo: Should we try to wait a bit the Ninja cache update before to take any action ?
                            RecordTask(task, QueuedTaskStatus.Error);
                            return;
                        }

                        RecordTask(task, QueuedTaskStatus.CancelRequested);
                        ninja.CancelTask(task.NinjaState.Id);
                    }
                    else
                    {
                        RecordTask(task, QueuedTaskStatus.CancelPending);
                    }
                }
                else // The task is pending
                {
                    var count = _pendingTasks.Count;
                    for (var i = 0; i < count; i++)
                    {
                        task = _pendingTasks.Dequeue();
                        if (task.Id == id)
                        {
                            RecordTask(task, QueuedTaskStatus.Completed);
                            continue;
                        }
                        _pendingTasks.Enqueue(task);
                    }
                }
            }
        }

        public void Refresh()
        {
            lock (_lock)
            {
                var runningTasks = _runningTasks.Values.ToList();
                foreach (var task in runningTasks)
                {
                    if (string.IsNullOrEmpty(task.NinjaAddress)) // Skip pending tasks
                        continue;

                    var ninja = _dojo.GetNinja(task.NinjaAddress);
                    if (ninja == null) continue; // TODO: Should we consider a state after a long run without ninja up like a timeout and allow delete the task ?

                    if (task.NinjaState == null)
                    {
                        // TODO: Should we remove this task because it is not known by the Ninja
                        // Todo: Should we try to wait a bit the Ninja cache update before to take any action ?
                        RecordTask(task, QueuedTaskStatus.Error);
                        continue;
                    }

                    var state = ninja.GetTaskState(task.NinjaState.Id);
                    if (state != null)
                    {
                        task.NinjaState = state;
                        if (state.IsFinal())
                        {
                            lock (_lock)
                                _runningTasks.Remove(task.Id);
                            RecordTask(task, QueuedTaskStatus.Completed);
                            
                            ninja.DeleteTask(task.NinjaState.Id);
                        }
                        else if (task.Status == QueuedTaskStatus.CancelPending)
                        {
                            ninja.CancelTask(task.NinjaState.Id);
                            RecordTask(task, QueuedTaskStatus.CancelRequested);
                        }
                        else RecordTask(task, task.Status);
                    }
                }

                var nbTasksToStart = _maxParallelTasks <= 0
                    ? _pendingTasks.Count
                    : _maxParallelTasks - _runningTasks.Count;
                DequeueTasks(nbTasksToStart);
            }
        }

        public bool DeleteTask(Guid id)
        {
            QueuedTaskDto task;
            lock (_lock)
            {
                if (!_tasks.TryGetValue(id, out task) || !task.IsFinalStatus())
                    return false;
            }

            // Delete the task from the Ninja
            if (!string.IsNullOrEmpty(task.NinjaAddress)) 
            {
                var ninja = _dojo.GetNinja(task.NinjaAddress);
                if (ninja == null || task.NinjaState == null) 
                    return false; // Todo: Should mark as deleted for a deletion later or enforce the delete if no Ninja can remind it existency

                ninja.DeleteTask(task.NinjaState.Id);
            }
            
            _tasks.Remove(id);
            _database.DeleteTask(id);
            return true;
        }

        private void DequeueTasks(int nbTasksToStart)
        {
            if (nbTasksToStart <= 0) return;

            for (var i = 0; i < nbTasksToStart; i++)
            {
                var task = _pendingTasks.Peek();
                StartTask(task);

                if (task.NinjaState != null && task.NinjaState.Id != Guid.Empty)
                    _pendingTasks.Dequeue();
                else break;
            }
        }
    }
}
