using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Domain;
using Domain.Dtos;
using Domain.Entities;


namespace Application.Ninja
{
    public class RunningTask : IDisposable
    {
        private readonly Process _process;
        private readonly List<TaskMessageDto> _messages = new List<TaskMessageDto>();
        private DateTime _startTime;
        private DateTime _endTime;

        public Guid Id { get; } = Guid.NewGuid();

        public int Pid => _process != null && _startTime != default ? _process.Id : -1;

        public string Command { get; }

        public string Arguments { get; }

        public List<TaskMessageDto> Messages => _messages;

        public DateTime StartTime => _startTime;

        public TaskStatus Status { get; private set; } = TaskStatus.Pending;

        public DateTime EndTime => _endTime;

        public string WorkingFolder { get; }

        public bool IsAlive => _process != null && !_process.HasExited;

        public event Action<RunningTask> Exited;

        public RunningTask(string command, string arguments, string workingFolder)
        {
            Command = command;
            Arguments = arguments;
            WorkingFolder = Path.Combine(workingFolder, Id.ToString("N"));

            _process = CreateProcess(command, arguments, WorkingFolder);
            _process.Exited += OnExited;
            _process.OutputDataReceived += OnOutpuDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
        }

        public RunningTask(TaskEntity entity)
        {
            Id = entity.Id;
            Command = entity.Command;
            Arguments = entity.Arguments;
            Status = entity.Status;
            WorkingFolder = entity.WorkingFolder;
            _startTime = entity.StartTime;
            _endTime = entity.EndTime;

            switch (Status)
            {
                case TaskStatus.Pending:
                    _process = CreateProcess(Command, Arguments, WorkingFolder);
                    break;
                case TaskStatus.Running:
                    try { _process = Process.GetProcessById(entity.Pid); } catch(Exception) { }
                    break;
                case TaskStatus.Done:
                case TaskStatus.Cancel:
                case TaskStatus.Error:
                case TaskStatus.EndedWithoutSupervision:
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

            Status = TaskStatus.Running;
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
                if (!Status.IsFinal()) Exit();
                return;
            }

            _process.Kill();
            _process.WaitForExit();
            Status = TaskStatus.Cancel;
        }

        public void Exit()
        {
            if (!Status.IsFinal())
            {
                if (_process == null)
                    Status = TaskStatus.EndedWithoutSupervision;
                else
                    Status = _process.ExitCode == 0 ? TaskStatus.Done : TaskStatus.Error;
                _endTime = DateTime.UtcNow;
            }
            
            PostMessage(MessageType.Exit, $"Exit with cose: {_process?.ExitCode}");
            Exited?.Invoke(this);
        }

        public TaskDto ToDto() => new TaskDto
        {
            Id = Id,
            StartTime = _startTime,
            Status = Status,
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
            => _messages.Add(new TaskMessageDto(Id, DateTime.UtcNow, type, message));

        public void Dispose()
        {
            Cancel();
            try { _process?.Dispose(); } catch(Exception) { }
            try { WorkingFolder.DeleteFolder(); } catch (Exception) { }
        }
    }
}
