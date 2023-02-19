using Domain.Dtos;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaskStatus = Domain.Dtos.TaskStatus;

namespace Application.Colony
{
    public class Beehive
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDispatcher _sequencer;
        private readonly Queue<RemoteTaskDto> _pendingTasks = new Queue<RemoteTaskDto>();
        private readonly Dictionary<Guid, RemoteTaskDto> _runningTasks = new Dictionary<Guid, RemoteTaskDto>();
        private readonly ConcurrentDictionary<Guid, RemoteTaskDto> _tasks = new ConcurrentDictionary<Guid, RemoteTaskDto>();
        private readonly BeeKeeper _beeKeeper;
        private readonly IColonyDb _database;
        private readonly TaskTracker _tracker;
        private int _maxParallelTasks;
        private HashSet<string> _bees = new HashSet<string>();
        private ulong _orderIncrement;
        private Queue<RemoteTaskDto> _tasksToSynchronize = new Queue<RemoteTaskDto>();

        public string Name => Dto.Name;

        public BeehiveDto Dto { get; } = new BeehiveDto();

        public Beehive(BeehiveDto dto, BeeKeeper beeKeeper, IDispatcherFactory dispatcherFactory, IColonyDb database, TaskTracker tracker)
        {
            _beeKeeper = beeKeeper;
            _sequencer = dispatcherFactory.CreateSequencer(dto.Name);
            _database = database;
            _tracker = tracker;
            Dto.Name = dto.Name;
            UpdateSync(dto);

            var tasks = _database.FetchTasks() ?? Enumerable.Empty<RemoteTaskDto>();
            RestoreTasks(tasks.Where(p => p.Beehive == Name));
        }

        public IEnumerable<RemoteTaskDto> GetAllTasks()
        {
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
            => _sequencer.Dispatch(() => UpdateSync(dto));

        private void UpdateSync(BeehiveDto dto)
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

        #region Start

        public Guid StartTask(string name, TaskParameters parameters)
        {
            var task = new RemoteTaskDto(Name, name, parameters, Interlocked.Increment(ref _orderIncrement));
            _sequencer.Dispatch(() => StartTask(task));
            return task.Id;
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

        #endregion

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

        #region Cancel

        public void CancelTask(Guid id)
        {
            _sequencer.Dispatch(() =>
            {
                Logger.InfoFormat("[{0}] Cancelling task with Id: {1}", Name, id);

                if (_runningTasks.TryGetValue(id, out var task))
                    CancelRunningTask(task);
                else CancelPendingTask(id);
            });
        }

        private void CancelRunningTask(RemoteTaskDto task)
        {
            Logger.InfoFormat("[{0}] Cancelling running task {1}", Name, task);

            var bee = _beeKeeper.GetBee(task.BeeAddress);
            if (bee == null)
            {
                Logger.InfoFormat("[{0}] The bee {1} cannot be reach out to cancel task {2}, we wait a bit that the Bee shows up", Name, task.BeeAddress, task);
                RecordTask(task, RemoteTaskStatus.CancelPending);
                return;
            }

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

        private void CancelPendingTask(Guid id)
        {
            Logger.InfoFormat("[{0}] Cancelling pending task with Id: {1}", Name, id);

            var count = _pendingTasks.Count;
            var found = false;
            for (var i = 0; i < count; i++)
            {
                var task = _pendingTasks.Dequeue();
                if (task.Id == id)
                {
                    found = true;
                    RecordTask(task, RemoteTaskStatus.Cancel);
                    continue;
                }
                _pendingTasks.Enqueue(task);
            }

            if (!found)
                Logger.WarnFormat("[{0}] The task was not found in pending queue: {1}", Name, id);
        }

        #endregion 

        #region Refresh

        public void Refresh()
        {
            _sequencer.Dispatch(() =>
            {
                UpdateRunningTasks();
                UpdateTasksToSynchronize();
                UpdatePendingTasks();
            });
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

        #region Delete

        public void DeleteTask(Guid id)
        {
            _sequencer.Dispatch(() =>
            {
                Logger.InfoFormat("[{0}] Deleting task with Id: {1}", Name, id);

                if (!_tasks.TryGetValue(id, out var task) || !task.IsFinalStatus())
                {
                    Logger.InfoFormat("[{0}] Cannot delete task with ID: {1}, because the task is not found or not in a final status. {2}", Name, id, task);
                    return;
                }

                // Delete the task from the Bee
                if (!string.IsNullOrEmpty(task.BeeAddress))
                {
                    var bee = _beeKeeper.GetBee(task.BeeAddress);
                    if (bee == null || task.BeeState == null)
                    {
                        Logger.InfoFormat("[{0}] Cannot find the Bee: {1 to delete task: {2}", Name, task.BeeAddress, task);
                        return; // Todo: Should mark as deleted for a later deletion or enforce the delete if no Bee can remind it existency
                    }

                    Logger.InfoFormat("[{0}] Deleting task with Id: {1} from Bee {2} with a BeeId: {3}.", Name, id, bee.Address, task.BeeState.Id);
                    bee.DeleteTask(task.BeeState.Id);
                }

                RecordTask(task, RemoteTaskStatus.Deleted);
                _tasks.TryRemove(id, out var _);

                Logger.InfoFormat("[{0}] Task with Id: {1} deleted.", Name, id);

                return;
            });
        }

        #endregion

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
            
            if (!_tasks.TryGetValue(id, out task))
                return new List<TaskMessageDto>();

            if(!TryGetBeeTaskState(task, out bee, out state))
                return task.Messages;

            var messages = bee.FetchMessages(state.Id);
            if (messages == null)
                return task.Messages;

            lock (task.Messages)
                task.Messages.AddRange(messages);

            return task.Messages;           
        }
    }
}
