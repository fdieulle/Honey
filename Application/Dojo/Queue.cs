using Domain;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Queue
    {
        private readonly Queue<PendingTaskDto> _pendingTasks = new Queue<PendingTaskDto>();
        private readonly Dictionary<Guid, Ninja> _runningTasks = new Dictionary<Guid, Ninja>();
        private readonly Dictionary<Guid, TaskDto> _tasks = new Dictionary<Guid, TaskDto>();
        private readonly Dojo _dojo;
        private int _maxParallelTasks;
        private HashSet<string> _ninjas = new HashSet<string>();

        public string Name => Dto.Name;

        public QueueDto Dto { get; } = new QueueDto();

        public IEnumerable<PendingTaskDto> PendingTasks => _pendingTasks;

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
            DequeTasks(nbTasksToStart);
        }

        public Guid StartTask(StartTaskDto task)
        {
            // Make sure we didn't exceed the max number of parallel tasks
            if (_maxParallelTasks > 0 && _runningTasks.Count >= _maxParallelTasks)
                return Hang(task);

            var ninja = _dojo.GetNextNinja(_ninjas);
            // If there is no available ninjas we enqueue the task
            if (ninja == null)
                return Hang(task);

            var id = ninja.StartTask(task.Command, task.Arguments, task.NbCores);
            // If the task start failed we will retry later
            if (id == default)
            {
                // Retry with another ninja by banning the faulted one
                var ninjas = new HashSet<string>(_ninjas);
                ninjas.Remove(ninja.Address);
                while (ninjas.Count > 0)
                {
                    ninja = _dojo.GetNextNinja(ninjas);
                    // If there is no available ninjas we enqueue the task
                    if (ninja == null)
                        return Hang(task);

                    id = ninja.StartTask(task.Command, task.Arguments, task.NbCores);
                    if (id == default)
                        ninjas.Remove(ninja.Address);
                    else break;
                    
                    if (ninjas.Count == 0)
                        return Hang(task);
                }
            }

            _runningTasks[id] = ninja;
            return id;
        }
        
        private Guid Hang(StartTaskDto task)
        {
            var pendingTask = new PendingTaskDto(task);
            _pendingTasks.Enqueue(pendingTask);
            return pendingTask.Id;
        }

        public void CancelTask(Guid id)
        {
            if (_runningTasks.TryGetValue(id, out var ninja))
                ninja.CancelTask(id);
            else
            {
                var count = _pendingTasks.Count;
                for(var i=0; i < count; i++)
                {
                    var task = _pendingTasks.Dequeue();
                    if (task.Id == id) continue;
                    _pendingTasks.Enqueue(task);
                }
            }
        }

        public void Refresh()
        {
            var endedTasks = new List<Guid>();
            foreach(var pair in _runningTasks)
            {
                var state = pair.Value.GetTaskState(pair.Key);
                if (state != null)
                {
                    _tasks[pair.Key] = state;
                    if (state.Status.IsFinal())
                        endedTasks.Add(pair.Key);
                }
                else
                {
                    // Todo: Handle this case ?
                }
            }

            foreach(var task in endedTasks)
                _runningTasks.Remove(task);

            var nbTasksToStart = _maxParallelTasks <= 0
                ? _pendingTasks.Count
                : _maxParallelTasks - _runningTasks.Count;
            DequeTasks(nbTasksToStart);
        }

        private void DequeTasks(int nbTasksToStart)
        {
            if (nbTasksToStart <= 0) return;

            for (var i = 0; i < nbTasksToStart; i++)
                StartTask(_pendingTasks.Dequeue().Task);
        }
    }
}
