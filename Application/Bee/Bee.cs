using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Domain.Dtos;
using log4net;
using System.Threading.Tasks;

namespace Application.Bee
{
    public class Bee : IBee, IFlower, IDisposable
    {
        private const int WATCH_DOG_PERIOD = 5000;

        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<Guid, RunningTask> _runningTasks = new Dictionary<Guid, RunningTask>();
        private readonly IBeeDb _database;
        private readonly IBeeResourcesProvider _beeResourcesProvider;
        private readonly string _workingFolder;
        private readonly string _workingDrive;
        private readonly Timer _timer;
        private readonly ProcessorAllocator _processorAllocator = new ProcessorAllocator();
        private bool _isDisposed;

        public Bee(
            IConfiguration config,
            IBeeDb database,
            IBeeResourcesProvider beeResourcesProvider)
        {
            _database = database;
            _beeResourcesProvider = beeResourcesProvider;
            _workingFolder = Path.Combine(config["WorkingFolder"] ?? ".", "tasks").CreateFolder();
            _workingDrive = Path.GetPathRoot(Path.GetFullPath(_workingFolder)).Replace("\\", "");
            _timer = new Timer(OnWatchDogWalk, null, WATCH_DOG_PERIOD, Timeout.Infinite);

            foreach(var task in _database.FetchTasks())
            {
                task.Exited += OnTaskExited;
                _runningTasks[task.Id] = task;
            }
        }

        public IEnumerable<TaskDto> GetTasks()
        {
            lock(_runningTasks)
            {
                return _runningTasks
                    .Select(p => p.Value.ToDto())
                    .ToList();
            }
        }

        public async Task<List<string>> FetchLogsAsync(Guid id, int start = 0, int length = -1)
        {
            if (!_runningTasks.TryGetValueLocked(id, out var task))
            {
                Logger.ErrorFormat("[{0}] Cannot find the task to fetch messages.", id);
                return new List<string>();
            }

            return await task.FetchLogsAsync(start, length);
        }
        
        public Guid StartTask(string command, string arguments, int nbCores = -1)
        {
            var task = new RunningTask(
                _beeResourcesProvider.GetBaseUri(), 
                command, arguments, _workingFolder);

            Logger.InfoFormat("Start a new task with Id={0}", task.Id);
            Logger.InfoFormat("[{0}] Command line: {1} {2}", task.Id, command, arguments);

            task.Exited += OnTaskExited;
            lock (_runningTasks)
            {
                _runningTasks[task.Id] = task;
                _database.CreateTask(task);
            }            

            try
            {
                task.Start();
            }
            catch (Exception e)
            {
                Logger.Error($"[{task.Id}] Cannot start the task", e);
                task.PostMessage(MessageType.Error, e.Message);
            }

            lock (_runningTasks)
            {
                _database.UpdateTask(task);
                _processorAllocator.SetAffinity(task.Pid, nbCores);
            }
            
            return task.Id;
        }

        public void CancelTask(Guid id)
        {
            if (!_runningTasks.TryGetValueLocked(id, out var task))
            {
                Logger.ErrorFormat("[{0}] Cannot find the task to cancel.", id);
                return;
            }
            
            task.Cancel();
        }

        public void DeleteTask(Guid id)
        {
            if (!_runningTasks.TryGetValueLocked(id, out var task))
            {
                Logger.ErrorFormat("[{0}] Cannot find the task to delete.", id);
                return;
            }

            task.Dispose();
            task.Exited -= OnTaskExited;
            _runningTasks.Remove(id);
            _database.DeleteTask(id);
        }

        private void OnTaskExited(RunningTask task)
        {
            task.Exited -= OnTaskExited;
            lock (_runningTasks)
            {
                _database.UpdateTask(task);
                _processorAllocator.RemoveProcess(task.Pid);
            }
        }

        private void OnWatchDogWalk(object state)
        {
            List<RunningTask> tasks;
            lock(_runningTasks)
            {
                tasks = _runningTasks.Values.Where(j => !j.Status.IsFinal() && !j.IsAlive).ToList();
            }

            foreach(var task in tasks)
                task.Exit();

            if (!_isDisposed)
                _timer.Change(WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public void Dispose()
        {
            _isDisposed = true;
            _timer.Dispose();
        }

        public BeeResourcesDto GetResources()
        {
            var memory = _beeResourcesProvider.GetPhysicalMemory();
            var disk = _beeResourcesProvider.GetDiskSpace(_workingDrive);

            return new BeeResourcesDto
            {
                MachineName = _beeResourcesProvider.GetMachineName(),
                OSPlatform = _beeResourcesProvider.GetOSPlatform(),
                OSVersion = _beeResourcesProvider.GetOSVersion(),
                NbCores = _processorAllocator.NbCores,
                NbFreeCores = _processorAllocator.NbFreeCores,
                TotalPhysicalMemory = memory.Total,
                AvailablePhysicalMemory = memory.Free,
                DiskSpace = disk.Total,
                DiskFreeSpace = disk.Free,
            };
        }

        public void UpdateTask(TaskStateDto dto)
        {
            if (dto == null) return;

            lock (_runningTasks)
            {
                if (_runningTasks.TryGetValue(dto.TaskId, out var task))
                    task.Update(dto.ProgressPercent, dto.ExpectedEndTime, dto.Message);
            }
        }
    }
}
