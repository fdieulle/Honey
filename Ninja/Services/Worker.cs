﻿using Hardware.Info;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Yumi;
using Ninja.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ninja.Services
{
    public class Worker : IDisposable
    {
        private const int WATCH_DOG_PERIOD = 5000;
        private static readonly IHardwareInfo hardwareInfo = new HardwareInfo(useAsteriskInWMI: false);

        private readonly Dictionary<Guid, RunningJob> _runningJobs = new Dictionary<Guid, RunningJob>();
        private readonly ILogger<Worker> _logger;
        private readonly IDbContextFactory<WorkerContext> _contextFactory;
        private readonly string _workingFolder;
        private readonly string _workingDrive;
        private readonly Timer _timer;
        private readonly ProcessorAllocator _processorAllocator = new ProcessorAllocator();
        private bool _isDisposed;

        public Worker(ILogger<Worker> logger, IConfiguration config, IDbContextFactory<WorkerContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _workingFolder = Path.Combine(config["WorkingFolder"] ?? ".", "jobs").CreateFolder(logger);
            _workingDrive = Path.GetPathRoot(Path.GetFullPath(_workingFolder)).Replace("\\", "");
            _timer = new Timer(OnWatchDogWalk, null, WATCH_DOG_PERIOD, Timeout.Infinite);

            foreach(var job in contextFactory.ReloadJobs())
            {
                job.Exited += OnJobExited;
                _runningJobs[job.Id] = job;
            }
        }

        public IEnumerable<Job> GetJobs()
        {
            lock(_runningJobs)
            {
                return _runningJobs
                    .Select(p => p.Value.ToDto())
                    .ToList();
            }
        }

        public IEnumerable<JobMessage> FetchMessages(Guid id, int start, int length)
        {
            if (!_runningJobs.TryGetValueLocked(id, out var job))
            {
                _logger.LogError("[{0}] Cannot find the job to fetch messages.", id);
                return Enumerable.Empty<JobMessage>();
            }

            if (start >= job.Messages.Count)
                return Enumerable.Empty<JobMessage>();

            length = Math.Min(job.Messages.Count - start, length);
            return job.Messages.Skip(start).Take(length);
        }

        public Guid StartJob(string command, string arguments, int nbCores = -1)
        {
            var job = new RunningJob(command, arguments, _workingFolder);

            _logger.LogInformation("Start a new job with Id={0}", job.Id);
            _logger.LogInformation("[{0}] Command line: {1} {2}", job.Id, command, arguments);

            job.Exited += OnJobExited;
            lock (_runningJobs)
            {
                _runningJobs[job.Id] = job;
                _contextFactory.CreateJob(job);
            }            

            try
            {
                job.Start();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[{0}] Cannot start the job", job.Id);
                job.PostMessage(MessageType.Error, e.Message);
            }

            lock (_runningJobs)
            {
                _contextFactory.UpdateJob(job);
                _processorAllocator.SetAffinity(job.Pid, nbCores);
            }
            

            return job.Id;
        }

        public void CancelJob(Guid id)
        {
            if (!_runningJobs.TryGetValueLocked(id, out var job))
            {
                _logger.LogError("[{0}] Cannot find the job to cancel.", id);
                return;
            }
            
            job.Cancel();

            lock (_runningJobs)
                _contextFactory.UpdateJob(job);
        }

        public void DeleteJob(Guid id)
        {
            if (!_runningJobs.TryGetValueLocked(id, out var job))
            {
                _logger.LogError("[{0}] Cannot find the job to delete.", id);
                return;
            }

            job.Dispose();
            job.Exited -= OnJobExited;
            _runningJobs.Remove(id);
            _contextFactory.DeleteJob(id);
        }

        private void OnJobExited(RunningJob job)
        {
            lock(_runningJobs)
            {
                _contextFactory.UpdateJob(job);
                _processorAllocator.RemoveProcess(job.Pid);
            }
        }
        

        private void OnWatchDogWalk(object state)
        {
            lock(_runningJobs)
            {
                foreach (var job in _runningJobs.Values.Where(j => !j.State.IsFinal() && !j.IsAlive))
                {
                    job.Exit();
                    _contextFactory.UpdateJob(job);
                }
            }

            if(!_isDisposed)
                _timer.Change(WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public void Dispose()
        {
            _isDisposed = true;
            _timer.Dispose();
        }


        public WorkerResources GetResources()
        {
            hardwareInfo.RefreshMemoryStatus();
            hardwareInfo.RefreshDriveList();

            var disk = hardwareInfo.DriveList
                .SelectMany(p => p.PartitionList)
                .SelectMany(p => p.VolumeList)
                .FirstOrDefault(p => p.Name == _workingDrive);

            return new WorkerResources
            {
                MachineName = Environment.MachineName,
                OSPlatform = Environment.OSVersion.Platform.ToString(),
                OSVersion = Environment.OSVersion.Version.ToString(),
                NbCores = _processorAllocator.NbCores,
                NbFreeCores = _processorAllocator.NbCores - _processorAllocator.NbUsedCores,
                TotalPhysicalMemory = hardwareInfo.MemoryStatus.TotalPhysical,
                AvailablePhysicalMemory = hardwareInfo.MemoryStatus.AvailablePhysical,
                DiskSpace = disk.Size,
                DiskFreeSpace = disk.FreeSpace,
            };
        }
    }
}
