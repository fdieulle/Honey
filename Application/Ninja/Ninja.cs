using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Domain.Dtos;

namespace Application.Ninja
{
    public class Ninja : INinja, INinjaClient, IDisposable
    {
        private const int WATCH_DOG_PERIOD = 5000;

        private readonly Dictionary<Guid, RunningTask> _runningTasks = new Dictionary<Guid, RunningTask>();
        private readonly ILogger<Ninja> _logger;
        private readonly INinjaDb _database;
        private readonly INinjaResourcesProvider _ninjaResourcesProvider;
        private readonly string _workingFolder;
        private readonly string _workingDrive;
        private readonly Timer _timer;
        private readonly ProcessorAllocator _processorAllocator = new ProcessorAllocator();
        private bool _isDisposed;

        public Ninja(
            ILogger<Ninja> logger, 
            IConfiguration config,
            INinjaDb database,
            INinjaResourcesProvider ninjaResourcesProvider)
        {
            _logger = logger;
            _database = database;
            _ninjaResourcesProvider = ninjaResourcesProvider;
            _workingFolder = Path.Combine(config["WorkingFolder"] ?? ".", "tasks").CreateFolder(logger);
            _workingDrive = Path.GetPathRoot(Path.GetFullPath(_workingFolder)).Replace("\\", "");
            _timer = new Timer(OnWatchDogWalk, null, WATCH_DOG_PERIOD, Timeout.Infinite);

            foreach(var job in _database.FetchTasks())
            {
                job.Exited += OnTaskExited;
                _runningTasks[job.Id] = job;
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

        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length)
        {
            if (!_runningTasks.TryGetValueLocked(id, out var task))
            {
                _logger.LogError("[{0}] Cannot find the task to fetch messages.", id);
                return Enumerable.Empty<TaskMessageDto>();
            }

            if (start >= task.Messages.Count)
                return Enumerable.Empty<TaskMessageDto>();

            length = Math.Min(task.Messages.Count - start, length);
            return task.Messages.Skip(start).Take(length);
        }

        public Guid StartTask(string command, string arguments, int nbCores = -1)
        {
            var task = new RunningTask(
                _ninjaResourcesProvider.GetBaseUri(), 
                command, arguments, _workingFolder);

            _logger.LogInformation("Start a new task with Id={0}", task.Id);
            _logger.LogInformation("[{0}] Command line: {1} {2}", task.Id, command, arguments);

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
                _logger.LogError(e, "[{0}] Cannot start the task", task.Id);
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
                _logger.LogError("[{0}] Cannot find the task to cancel.", id);
                return;
            }
            
            task.Cancel();
        }

        public void DeleteTask(Guid id)
        {
            if (!_runningTasks.TryGetValueLocked(id, out var task))
            {
                _logger.LogError("[{0}] Cannot find the task to delete.", id);
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

        public NinjaResourcesDto GetResources()
        {
            var memory = _ninjaResourcesProvider.GetPhysicalMemory();
            var disk = _ninjaResourcesProvider.GetDiskSpace(_workingDrive);

            return new NinjaResourcesDto
            {
                MachineName = _ninjaResourcesProvider.GetMachineName(),
                OSPlatform = _ninjaResourcesProvider.GetOSPlatform(),
                OSVersion = _ninjaResourcesProvider.GetOSVersion(),
                NbCores = _processorAllocator.NbCores,
                NbFreeCores = _processorAllocator.NbCores - _processorAllocator.NbUsedCores,
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
