using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ninja.Dto;
using Ninja.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ninja.Services
{
    public class Worker : IDisposable
    {
        private const int WATCH_DOG_PERIOD = 5000;

        private readonly Dictionary<string, RunningJob> _runningJobs = new Dictionary<string, RunningJob>();
        private readonly ILogger<Worker> _logger;
        private readonly string _workingFolder;
        private readonly Timer _timer;
        private bool _isDisposed;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _workingFolder = config["WorkingFolder"].CreateFolder(logger);
            _timer = new Timer(OnWatchDogWalk, null, WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public IEnumerable<Job> GetJobs()
        {
            return _runningJobs
                .Select(p => p.Value.ToDto());
        }

        public IEnumerable<JobMessage> FetchMessages(string id, int start, int length)
        {
            if (!_runningJobs.TryGetValue(id, out var job))
            {
                _logger.LogError("[{0}] Cannot find the job to fetch messages.", id);
                return Enumerable.Empty<JobMessage>();
            }

            if (start >= job.Messages.Count)
                return Enumerable.Empty<JobMessage>();

            length = Math.Min(job.Messages.Count - start, length);
            return job.Messages.Skip(start).Take(length);
        }

        public string StartJob(string command, string arguments, int nbCores = -1)
        {
            var job = new RunningJob(command, arguments, _workingFolder);
            _logger.LogInformation("Start a new job with Id={0}", job.Id);
            _logger.LogInformation("[{0}] Command line: {1} {2}", job.Id, command, arguments);
            _runningJobs[job.Id] = job;

            try
            {
                job.Start();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[{0}] Cannot start the job", job.Id);
                job.PostMessage(MessageType.Error, e.Message);
            }

            return job.Id;
        }

        public void CancelJob(string id)
        {
            if (!_runningJobs.TryGetValue(id, out var job))
            {
                _logger.LogError("[{0}] Cannot find the job to cancel.", id);
                return;
            }

            job.Cancel();
        }

        public void DeleteJob(string id)
        {
            if (!_runningJobs.TryGetValue(id, out var job))
            {
                _logger.LogError("[{0}] Cannot find the job to delete.", id);
                return;
            }

            job.Cancel();
            _runningJobs.Remove(id);
        }

        private void OnWatchDogWalk(object state)
        {
            foreach (var job in _runningJobs.Values)
                if (!job.State.IsFinal() && job.IsAlive)
                    job.Exit();

            if(!_isDisposed)
                _timer.Change(WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public void Dispose()
        {
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
