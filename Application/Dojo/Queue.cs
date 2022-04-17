using Domain;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Queue
    {
        private readonly Queue<QueueTaskDto> _pendingTasks = new Queue<QueueTaskDto>();
        private readonly Dictionary<Guid, QueueTaskDto> _runningTasks = new Dictionary<Guid, QueueTaskDto>();
        private readonly Dictionary<Guid, QueueTaskDto> _tasks = new Dictionary<Guid, QueueTaskDto>();
        private readonly Dojo _dojo;
        private int _maxParallelTasks;
        private HashSet<string> _ninjas = new HashSet<string>();

        public string Name => Dto.Name;

        public QueueDto Dto { get; } = new QueueDto();

        public IEnumerable<QueueTaskDto> PendingTasks => _pendingTasks;

        public Queue(QueueDto dto, Dojo dojo)
        {
            Dto = dto.Clone();
            _dojo = dojo;
        }

        public void Update(QueueDto dto)
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

        public Guid StartTask(StartTaskDto startTask)
        {
            return StartTask(new QueueTaskDto(startTask));
        }

        private Guid StartTask(QueueTaskDto task)
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
        
        private Guid RunTask(QueueTaskDto task, Ninja ninja, Guid ninjaTaskId)
        {
            task.NinjaState.Id = ninjaTaskId;
            task.NinjaAddress = ninja.Address;
            _runningTasks[task.Id] = task;
            RecordTask(task);
            return task.Id;
        }

        private Guid Hang(QueueTaskDto task)
        {
            if (!_tasks.ContainsKey(task.Id))
                _pendingTasks.Enqueue(task);
            RecordTask(task);
            return task.Id;
        }

        private void RecordTask(QueueTaskDto task)
        {
            _tasks[task.Id] = task;
        }

        public void CancelTask(Guid id)
        {
            // Try cancel running task
            if (_runningTasks.TryGetValue(id, out var task))
            {
                var ninja = _dojo.GetNinja(task.NinjaAddress);
                if (ninja != null)
                    ninja.CancelTask(task.NinjaState.Id);
                else
                {
                    // Todo: store the cancel request and retry when the ninja is available.
                }
            }
            else // The task is pending
            {
                var count = _pendingTasks.Count;
                for (var i = 0; i < count; i++)
                {
                    task = _pendingTasks.Dequeue();
                    if (task.Id == id) continue;
                    _pendingTasks.Enqueue(task);
                }
            }
        }

        public void Refresh()
        {
            var endedTasks = new List<Guid>();
            foreach(var task in _tasks.Values)
            {
                if (string.IsNullOrEmpty(task.NinjaAddress)) // Skip pending tasks
                    continue;

                var ninja = _dojo.GetNinja(task.NinjaAddress);
                if (ninja == null) continue; // TODO: Should we consider a state after a long run without ninja up ?

                var state = ninja.GetTaskState(task.NinjaState.Id);
                if (state != null)
                {
                    task.NinjaState = state;
                    if (state.IsFinal())
                        endedTasks.Add(task.Id);
                    else
                    {
                        // Todo: Handle this case ?
                    }

                }
            }

            foreach(var task in endedTasks)
                _runningTasks.Remove(task);

            var nbTasksToStart = _maxParallelTasks <= 0
                ? _pendingTasks.Count
                : _maxParallelTasks - _runningTasks.Count;
            DequeueTasks(nbTasksToStart);
        }

        private void DequeueTasks(int nbTasksToStart)
        {
            if (nbTasksToStart <= 0) return;

            for (var i = 0; i < nbTasksToStart; i++)
            {
                var task = _pendingTasks.Peek();
                StartTask(task);

                if (task.NinjaState.Id != Guid.Empty)
                    _pendingTasks.Dequeue();
                else break;
            }
        }
    }
}
