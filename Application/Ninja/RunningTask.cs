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
        private readonly TaskDto _taskDto = new TaskDto();
        private readonly List<TaskMessageDto> _messages = new List<TaskMessageDto>();
        private readonly string _baseUri;

        public Guid Id { get; } = Guid.NewGuid();

        public int Pid => _process != null && _taskDto.StartTime != default ? _process.Id : -1;

        public string Command { get; }

        public string Arguments { get; }

        public List<TaskMessageDto> Messages => _messages;

        public TaskStatus Status 
        {
            get => _taskDto.Status;
            private set => _taskDto.Status = value;
        }

        public string WorkingFolder { get; }

        public bool IsAlive => Status == TaskStatus.Pending || (_process != null && !_process.HasExited);

        public event Action<RunningTask> Exited;

        public RunningTask(string baseUri, string command, string arguments, string workingFolder)
        {
            _baseUri = baseUri;
            Command = command;
            Arguments = arguments;
            WorkingFolder = Path.Combine(workingFolder, Id.ToString("N"));

            _taskDto.Id = Id;
            if (!string.IsNullOrEmpty(command))
            {
                _taskDto.Status = TaskStatus.Pending;

                _process = CreateProcess(command, arguments, WorkingFolder);
                _process.Exited += OnExited;
                _process.OutputDataReceived += OnOutpuDataReceived;
                _process.ErrorDataReceived += OnErrorDataReceived;
            }
            else
            {
                _taskDto.Status = TaskStatus.Error;
                _taskDto.Message = "The command line is null or empty";
            }
        }

        public RunningTask(string baseUri, TaskEntity entity)
        {
            _baseUri = baseUri;
            Id = entity.Id;
            Command = entity.Command;
            Arguments = entity.Arguments;
            Status = entity.Status;
            WorkingFolder = entity.WorkingFolder;

            _taskDto.Id = Id;
            _taskDto.StartTime = entity.StartTime;
            _taskDto.EndTime = entity.EndTime;

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
                    break;
            }

            if (_process != null)
            {
                _process.Exited += OnExited;
                _process.OutputDataReceived += OnOutpuDataReceived;
                _process.ErrorDataReceived += OnErrorDataReceived;
            }
        }

        private Process CreateProcess(string command, string arguments, string workingFolder)
        {
            var startInfo = new ProcessStartInfo(command, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = workingFolder,
            };

            startInfo.EnvironmentVariables.Add("NINJA_TASK_ID", Id.ToString());
            startInfo.EnvironmentVariables.Add("NINJA_BASE_URI", _baseUri);

            return new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };
        }

        public void Start()
        {
            if (_process == null) return;

            PostMessage(MessageType.Info, $"Start job with id: {Id}");
            WorkingFolder.CreateFolder();

            _process.Start();
            _taskDto.StartTime = DateTime.UtcNow;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Status = TaskStatus.Running;
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
                Status = _process != null && _process.ExitCode == 0 
                    ? TaskStatus.Done 
                    : TaskStatus.Error;
                _taskDto.EndTime = DateTime.UtcNow;
            }
            
            PostMessage(MessageType.Exit, $"Exit with cose: {_process?.ExitCode}");
            Exited?.Invoke(this);
        }

        public void Update(double progressPercent, DateTime expectedEndTime, string message)
        {
            _taskDto.ProgressPercent = progressPercent;
            _taskDto.ExpectedEndTime = expectedEndTime;
            _taskDto.Message = message;
        }

        public TaskDto ToDto() => _taskDto.Clone();

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
