using Yumi;
using Ninja.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ninja.Services
{
    public class RunningJob : IDisposable
    {
        private readonly Process _process;
        private readonly List<JobMessage> _messages = new List<JobMessage>();
        private DateTime _startTime;
        private DateTime _endTime;

        public Guid Id { get; } = Guid.NewGuid();

        public int Pid => _process != null && _startTime != default ? _process.Id : -1;

        public string Command { get; }

        public string Arguments { get; }

        public List<JobMessage> Messages => _messages;

        public DateTime StartTime => _startTime;

        public JobState State { get; private set; } = JobState.Pending;

        public DateTime EndTime => _endTime;

        public string WorkingFolder { get; }

        public bool IsAlive => _process != null && !_process.HasExited;

        public event Action<RunningJob> Exited;

        public RunningJob(string command, string arguments, string workingFolder)
        {
            Command = command;
            Arguments = arguments;
            WorkingFolder = Path.Combine(workingFolder, Id.ToString("N"));

            _process = CreateProcess(command, arguments, WorkingFolder);
            _process.Exited += OnExited;
            _process.OutputDataReceived += OnOutpuDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
        }

        public RunningJob(JobModel model)
        {
            Id = model.Id;
            Command = model.Command;
            Arguments = model.Arguments;
            State = model.State;
            WorkingFolder = model.WorkingFolder;
            _startTime = model.StartTime;
            _endTime = model.EndTime;

            switch (State)
            {
                case JobState.Pending:
                    _process = CreateProcess(Command, Arguments, WorkingFolder);
                    break;
                case JobState.Running:
                    try { _process = Process.GetProcessById(model.Pid); } catch(Exception) { }
                    break;
                case JobState.Done:
                case JobState.Cancel:
                case JobState.Error:
                case JobState.EndedWithoutSupervision:
                    break;
            }

            if (_process != null)
            {
                _process.Exited += OnExited;
                _process.OutputDataReceived += OnOutpuDataReceived;
                _process.ErrorDataReceived += OnErrorDataReceived;
            }
        }

        private static Process CreateProcess(string command, string arguments, string workingFolder)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo(command, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = workingFolder,
                },
                EnableRaisingEvents = true,
            };
        }

        public void Start()
        {
            if (_process == null) return;

            PostMessage(MessageType.Info, $"Start job with id: {Id}");
            WorkingFolder.CreateFolder();

            State = JobState.Running;
            _process.Start();
            _startTime = DateTime.UtcNow;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Cancel()
        {
            if (_process == null) return;

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
            {
                if (_process == null)
                    State = JobState.EndedWithoutSupervision;
                else 
                    State = _process.ExitCode == 0 ? JobState.Done : JobState.Error;
                _endTime = DateTime.UtcNow;
            }
            
            PostMessage(MessageType.Exit, $"Exit with cose: {_process?.ExitCode}");
            Exited?.Invoke(this);
        }

        public Job ToDto() => new Job
        {
            Id = Id,
            StartTime = _startTime,
            State = State.ToString(),
            EndTime = _endTime,
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
            Cancel();
            try { _process?.Dispose(); } catch(Exception) { }
            try { WorkingFolder.DeleteFolder(); } catch (Exception) { }
        }
    }
}
