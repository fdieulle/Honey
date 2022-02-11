﻿using Ninja.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ninja.Services
{
    public class RunningJob : IDisposable
    {
        private readonly Process _process;
        private readonly string _workingFolder;
        private readonly List<JobMessage> _messages = new List<JobMessage>();

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public int Pid => _process.Id;

        public List<JobMessage> Messages => _messages;

        public JobState State { get; private set; } = JobState.Pending;

        public bool IsAlive => !_process.HasExited;

        public RunningJob(string command, string argumente, string workingFolder)
        {
            _workingFolder = Path.Combine(workingFolder, Id);

            _process = new Process
            {
                StartInfo = new ProcessStartInfo(command, argumente)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = _workingFolder,
                },
                EnableRaisingEvents = true,
            };
            _process.Exited += OnExited;
            _process.OutputDataReceived += OnOutpuDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
        }

        public void Start()
        {
            PostMessage(MessageType.Info, $"Start job with id: {Id}");
            _workingFolder.CreateFolder();

            State = JobState.Running;
            _process.Start();

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Cancel()
        {
            if (_process.HasExited)
            {
                if (!State.IsFinal()) Exit();
                return;
            }

            _process.Kill();
            _process.WaitForExit();
            State = JobState.Cancel;
        }

        public void Exit()
        {
            if (!State.IsFinal())
                State = _process.ExitCode == 0 ? JobState.Done : JobState.Error;
            PostMessage(MessageType.Exit, $"Exit with cose: {_process.ExitCode}");
        }

        public Job ToDto() => new Job
        {
            Id = Id,
            State = State.ToString(),
            Progress = -1,
            ExpectedEndTime = DateTime.MaxValue
        };

        private void OnOutpuDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                PostMessage(MessageType.Info, e.Data);
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                PostMessage(MessageType.Error, e.Data);
        }

        private void OnExited(object sender, EventArgs e) => Exit();

        public void PostMessage(MessageType type, string message) 
            => _messages.Add(new JobMessage(Id, DateTime.UtcNow, type, message));

        public void Dispose()
        {
            _process.Dispose();
            _workingFolder.DeleteFolder();
        }
    }
}
