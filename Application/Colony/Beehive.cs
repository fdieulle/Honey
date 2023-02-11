using Domain.Dtos;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Colony
{
    public class Beehive
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Queue<RemoteTaskDto> _pendingTasks = new Queue<RemoteTaskDto>();
        private readonly Dictionary<Guid, RemoteTaskDto> _runningTasks = new Dictionary<Guid, RemoteTaskDto>();
        private readonly Dictionary<Guid, RemoteTaskDto> _tasks = new Dictionary<Guid, RemoteTaskDto>();
        private readonly object _lock = new object();
        private readonly BeeKeeper _beeKeeper;
        private readonly IColonyDb _database;
        private readonly TaskTracker _tracker;
        private int _maxParallelTasks;
        private HashSet<string> _bees = new HashSet<string>();
        private ulong _orderIncrement;
        private Queue<RemoteTaskDto> _tasksToSynchronize = new Queue<RemoteTaskDto>();

        public string Name => Dto.Name;

        public BeehiveDto Dto { get; } = new BeehiveDto();

        public Beehive(BeehiveDto dto, BeeKeeper beeKeeper, IColonyDb database, TaskTracker tracker)
        {
            _beeKeeper = beeKeeper;
            _database = database;
            _tracker = tracker;
            Dto.Name = dto.Name;
            Update(dto);

            var tasks = _database.FetchTasks() ?? Enumerable.Empty<RemoteTaskDto>();
            RestoreTasks(tasks.Where(p => p.Beehive == Name));
        }

        public IEnumerable<RemoteTaskDto> GetAllTasks()
        {
            lock (_lock)
                return _tasks.Values.ToList();
        }

        private void RestoreTasks(IEnumerable<RemoteTaskDto> tasks)
        {
            var pendingTasks = new List<RemoteTaskDto>();
            foreach (var task in tasks)
            {
                _tasks[task.Id] = task;

                switch (task.Status)
                {
                    case RemoteTaskStatus.Pending:
                        pendingTasks.Add(task);
                        break;
                    case RemoteTaskStatus.Running:
                    case RemoteTaskStatus.CancelRequested:
                    case RemoteTaskStatus.CancelPending:
                        _runningTasks[task.Id] = task;
                        break;
                    default:
                        _tasksToSynchronize.Enqueue(task);
                        break;
                }
            }

            foreach (var task in pendingTasks.OrderBy(p => p.Order))
                _pendingTasks.Enqueue(task);
        }

        public void Update(BeehiveDto dto)
        {
            lock (_lock)
            {
                Dto.MaxParallelTasks = dto.MaxParallelTasks;
                Dto.Bees = new List<string>(dto.Bees ?? Enumerable.Empty<string>());

                _bees = new HashSet<string>(dto.Bees ?? Enumerable.Empty<string>());

                var current = _maxParallelTasks;
                _maxParallelTasks = dto.MaxParallelTasks;

                var nbTasksToStart = dto.MaxParallelTasks <= 0
                    ? _pendingTasks.Count
                    : dto.MaxParallelTasks - current;
                DequeueTasks(nbTasksToStart);
            }
        }

        public Guid StartTask(string name, TaskParameters startTask)
        {
            lock (_lock)
            {
                return StartTask(new RemoteTaskDto(Name, name, startTask, _orderIncrement++));
            }
        }

        private Guid StartTask(RemoteTaskDto task)
        {
            Logger.InfoFormat("[{0}] Starting task: {1}", Name, task);

            // Make sure we didn't exceed the max number of parallel tasks
            if (_maxParallelTasks > 0 && _runningTasks.Count >= _maxParallelTasks)
            {
                Logger.InfoFormat("[{0}] Max parallel tasks exceeded.", Name);
                return Hang(task);
            }

            var bees = new HashSet<string>(_bees);
            Bee bee;
            while ((bee = _beeKeeper.GetNextBee(bees)) != null)
            {
                Logger.InfoFormat("[{0}] Starting task {1} on Bee {2}", Name, task, bee);

                var beeId = bee.StartTask(task);
                // If a task has been launched we record it
                if (beeId != Guid.Empty)
                    return RunTask(task, bee, beeId);

                Logger.InfoFormat("[{0}] Cannot start task {1} on bee {2}. Trying another.", Name, task, bee);

                // Otherwise retry with another bee by banning the faulted one
                bees.Remove(bee.Address);

                // If there is no more usable bees we stop retries.
                if (bees.Count == 0)
                    break;
            }

            Logger.InfoFormat("[{0}] No Bee available to start task {1}.", Name, task, bee);

            // The task is enqueued with a pending state
            return Hang(task);
        }
        
        private Guid RunTask(RemoteTaskDto task, Bee bee, Guid beeTaskId)
        {
            Logger.InfoFormat("[{0}] Task {1} is running on Bee {2} with BeeTaskId: {3}", Name, task, bee, beeTaskId);

            task.BeeState.Id = beeTaskId;
            task.BeeAddress = bee.Address;

            _runningTasks[task.Id] = task;
            RecordTask(task, RemoteTaskStatus.Running);
            return task.Id;
        }

        private Guid Hang(RemoteTaskDto task)
        {
            Logger.InfoFormat("[{0}] Hang task: {1}", Name, task);

            if (!_tasks.ContainsKey(task.Id))
                _pendingTasks.Enqueue(task);

            RecordTask(task, RemoteTaskStatus.Pending);
            return task.Id;
        }

        private void RecordTask(RemoteTaskDto task, RemoteTaskStatus status)
        {
            var statusChanged = task.Status != status;
            if (statusChanged) 
            {
                task.Status = status;
                Logger.InfoFormat("[{0}] Task updated: {1}", Name, task);
            }

            if (!_tasks.ContainsKey(task.Id))
                _database.CreateTask(task);
            else if (status == RemoteTaskStatus.Deleted)
                _database.DeleteTask(task.Id);
            else if(statusChanged)
                _database.UpdateTask(task);

            _tasks[task.Id] = task;
            _tracker.Track(task);
        }

        public void CancelTask(Guid id)
        {
            lock (_lock)
            {
                Logger.InfoFormat("[{0}] Cancelling task with Id: {1}", Name, id);

                // Try cancel running task
                if (_runningTasks.TryGetValue(id, out var task))
                {
                    Logger.InfoFormat("[{0}] Cancelling running task {1}", Name, task);

                    var bee = _beeKeeper.GetBee(task.BeeAddress);
                    if (bee != null)
                    {
                        if (task.BeeState == null)
                        {
                            Logger.WarnFormat("[{0}] The Bee didn't give any status yet, We can only remove task {1}", Name, task);
                            // Todo: The Bee didn't give any status yet, We can only remove it
                            // Todo: Should we try to wait a bit the Bee cache update before to take any action ?
                            RecordTask(task, RemoteTaskStatus.Error);
                            return;
                        }

                        RecordTask(task, RemoteTaskStatus.CancelRequested);
                        bee.CancelTask(task.BeeState.Id);
                    }
                    else
                    {
                        Logger.InfoFormat("[{0}] The bee {1} cannot be reach out to cancel task {2}, we wait a bit that the Bee shows up", Name, task.BeeAddress, task);

                        RecordTask(task, RemoteTaskStatus.CancelPending);
                    }
                }
                else // The task is pending
                {
                    Logger.InfoFormat("[{0}] Cancelling pending task with Id: {1}", Name, id);

                    var count = _pendingTasks.Count;
                    for (var i = 0; i < count; i++)
                    {
                        task = _pendingTasks.Dequeue();
                        if (task.Id == id)
                        {
                            RecordTask(task, RemoteTaskStatus.Cancel);
                            continue;
                        }
                        _pendingTasks.Enqueue(task);
                    }
                }
            }
        }

        #region Refresh

        public void Refresh()
        {
            lock (_lock)
            {
                UpdateRunningTasks();
                UpdateTasksToSynchronize();
                UpdatePendingTasks();
            }
        }

        private void UpdateRunningTasks()
        {
            var tasks = _runningTasks.Values.ToList();
            foreach (var task in tasks)
            {
                if (!TryGetBeeTaskState(task, out var bee, out var state))
                    continue;

                task.BeeState = state;

                if (state.IsFinalStatus())
                {
                    _runningTasks.Remove(task.Id);

                    var status = state.Status;
                    if (status == TaskStatus.Done)
                        RecordTask(task, RemoteTaskStatus.Completed);
                    else if (status == TaskStatus.Cancel)
                        RecordTask(task, RemoteTaskStatus.Cancel);
                    else
                        RecordTask(task, RemoteTaskStatus.Error);

                    //bee.DeleteTask(task.BeeState.Id);
                }
                else if (task.Status == RemoteTaskStatus.CancelPending)
                {
                    bee.CancelTask(task.BeeState.Id);
                    RecordTask(task, RemoteTaskStatus.CancelRequested);
                }
                else RecordTask(task, task.Status);
            }
        }

        private void UpdateTasksToSynchronize()
        {
            var count = _tasksToSynchronize.Count;
            for(var i=0; i<count; i++)
            {
                var task = _tasksToSynchronize.Peek();

                if (!TryGetBeeTaskState(task, out var bee, out var state))
                    continue;
                
                task.BeeState = state;
                _tasksToSynchronize.Dequeue();
            }
        }

        private void UpdatePendingTasks()
        {
            var nbTasksToStart = _maxParallelTasks <= 0
                ? _pendingTasks.Count
                : _maxParallelTasks - _runningTasks.Count;
            DequeueTasks(nbTasksToStart);
        }

        private bool TryGetBeeTaskState(RemoteTaskDto task, out Bee bee, out TaskDto state)
        {
            state = null;
            bee = null;

            if (string.IsNullOrEmpty(task.BeeAddress)) // Skip pending tasks
                return false;

            bee = _beeKeeper.GetBee(task.BeeAddress);
            if (bee == null) return false; // TODO: Should we consider a state after a long run without bee up like a timeout and allow delete the task ?

            if (task.BeeState == null)
            {
                // TODO: Should we remove this task because it is not known by the Bee
                // Todo: Should we try to wait a bit the Bee cache update before to take any action ?
                RecordTask(task, RemoteTaskStatus.Error);
                return false;
            }

            state = bee.GetTaskState(task.BeeState.Id);
            return state != null;
        }

        #endregion

        public bool DeleteTask(Guid id)
        {
            lock (_lock)
            {
                if (!_tasks.TryGetValue(id, out var task) || !task.IsFinalStatus())
                    return false;

                // Delete the task from the Bee
                if (!string.IsNullOrEmpty(task.BeeAddress))
                {
                    var bee = _beeKeeper.GetBee(task.BeeAddress);
                    if (bee == null || task.BeeState == null)
                        return false; // Todo: Should mark as deleted for a deletion later or enforce the delete if no Bee can remind it existency

                    bee.DeleteTask(task.BeeState.Id);
                }

                RecordTask(task, RemoteTaskStatus.Deleted);
                _tasks.Remove(id);

                return true;
            }
        }

        private void DequeueTasks(int nbTasksToStart)
        {
            if (nbTasksToStart <= 0) return;

            for (var i = 0; i < nbTasksToStart; i++)
            {
                var task = _pendingTasks.Peek();
                StartTask(task);

                if (task.BeeState != null && task.BeeState.Id != Guid.Empty)
                    _pendingTasks.Dequeue();
                else break;
            }
        }

        public List<TaskMessageDto> FetchTaskMessages(Guid id)
        {
            RemoteTaskDto task;
            Bee bee;
            TaskDto state;
            lock(_tasks)
            {
                if (!_tasks.TryGetValue(id, out task))
                    return new List<TaskMessageDto>();

                if(!TryGetBeeTaskState(task, out bee, out state))
                    return task.Messages;
            }

            var messages = bee.FetchMessages(state.Id);
            if (messages == null)
                return task.Messages;

            lock (_tasks)
                task.Messages.AddRange(messages);

            return task.Messages;           
        }
    }
}
