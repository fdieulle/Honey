using Domain;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Queue
    {
        private readonly Queue<StartTaskDto> _pendingTasks = new Queue<StartTaskDto>();
        private readonly Dictionary<Guid, Ninja> _runningTasks = new Dictionary<Guid, Ninja>();
        private readonly Dictionary<Guid, TaskDto> _tasks = new Dictionary<Guid, TaskDto>();
        private readonly Dojo _dojo;
        private int _maxParallelTasks;
        private HashSet<string> _ninjas;

        public string Name { get; }

        public QueueDto Dto { get; } = new QueueDto();

        public IEnumerable<StartTaskDto> PendingTasks => _pendingTasks;

        public Queue(string name, Dojo dojo)
        {
            Name = name;
            _dojo = dojo;
        }

        public void Update(int maxParallelTask, IEnumerable<string> ninjas)
        {
            Dto.MaxParallelTasks = maxParallelTask;
            Dto.Ninjas = new List<string>(ninjas ?? Enumerable.Empty<string>());

            _ninjas = new HashSet<string>(ninjas ?? Enumerable.Empty<string>());

            var current = _maxParallelTasks;
            _maxParallelTasks = maxParallelTask;

            var nbTasksToStart = maxParallelTask <= 0 
                ? _pendingTasks.Count 
                : maxParallelTask - current;
            DequeTasks(nbTasksToStart);
        }

        public void StartTask(StartTaskDto task)
        {
            // Make sure we didn't exceed the max number of parallel tasks
            if (_maxParallelTasks > 0 && _runningTasks.Count >= _maxParallelTasks)
            {
                _pendingTasks.Enqueue(task);
                return;
            }

            var ninja = _dojo.GetNextNinja(_ninjas);
            // If there is no available ninjas we enqueue the task
            if (ninja == null)
            {
                _pendingTasks.Enqueue(task);
                return;
            }

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
                    {
                        _pendingTasks.Enqueue(task);
                        return;
                    }

                    id = ninja.StartTask(task.Command, task.Arguments, task.NbCores);
                    if (id == default)
                        ninjas.Remove(ninja.Address);
                    else break;
                    
                    if (ninjas.Count == 0)
                    {
                        _pendingTasks.Enqueue(task);
                        return;
                    }
                }
            }

            _runningTasks[id] = ninja;
        }
        
        public void CancelJob(Guid id)
        {
            if (_runningTasks.TryGetValue(id, out var ninja))
                ninja.CancelTask(id);
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
                StartTask(_pendingTasks.Dequeue());
        }
    }
}
