using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Domain.Dtos;
using Domain.Entities;


namespace Application.Bee
{
    public class RunningTask : IDisposable
    {
        private readonly Process _process;
        private readonly TaskDto _taskDto = new TaskDto();
        private readonly Queue<TaskMessageDto> _messages = new Queue<TaskMessageDto>();
        private readonly string _baseUri;
        private readonly StreamWriter _logFile;

        public Guid Id { get; } = Guid.NewGuid();

        public int Pid { get; private set; } = -1;

        public string Command { get; }

        public string Arguments { get; }

        public Queue<TaskMessageDto> Messages => _messages;

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

            _logFile = CreateLogFile(workingFolder, Id);

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
            Pid = entity.Pid;
            Command = entity.Command;
            Arguments = entity.Arguments;
            Status = entity.Status;
            WorkingFolder = entity.WorkingFolder;

            _logFile = CreateLogFile(WorkingFolder, Id);

            _taskDto.Id = Id;
            _taskDto.StartTime = entity.StartTime;
            _taskDto.EndTime = entity.EndTime;
            _taskDto.ProgressPercent = entity.ProgressPercent;

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

            startInfo.EnvironmentVariables.Add("BEE_TASK_ID", Id.ToString());
            startInfo.EnvironmentVariables.Add("BEE_BASE_URI", _baseUri);
            startInfo.EnvironmentVariables.Add("BEE_TASK_FOLDER", workingFolder);

            return new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };
        }

        public void Start()
        {
            if (_process == null) return;

            PostMessage(MessageType.Info, $"Starting job ...");
            PostMessage(MessageType.Info, $"[Id] {Id}");
            PostMessage(MessageType.Info, $"[Command] {Command} {Arguments}");
            PostMessage(MessageType.Info, $"[Working Forlder] {WorkingFolder}");

            try
            {
                WorkingFolder.CreateFolder();

                if (_process.Start())
                {
                    Pid = _process.Id;

                    PostMessage(MessageType.Info, $"[PID] {Pid}");

                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();

                    _taskDto.StartTime = DateTime.UtcNow;
                    Status = TaskStatus.Running;
                }
                else
                {
                    Status= TaskStatus.Error;
                    PostMessage(MessageType.Exit, "Start Failed");
                    // Todo: Send start failed reason
                }
            } 
            catch (Exception e)
            {
                Status = TaskStatus.Error;
                PostMessage(MessageType.Exit, Format(e));
            }
        }

        public void Cancel()
        {
            if (Status.IsFinal() || _process == null) return;

            if (_process.HasExited)
            {
                if (!Status.IsFinal()) 
                    Exit();
                return;
            }

            _process.Kill(true);
            _process.WaitForExit();
            Status = TaskStatus.Cancel;

            Exit();
        }

        public void Exit()
        {
            if (!Status.IsFinal())
            {
                if (_process != null)
                {
                    try
                    {
                        _process.WaitForExit();
                        Status = _process.ExitCode == 0 ? TaskStatus.Done : TaskStatus.Error;
                    }
                    catch(Exception e)
                    {
                        Status = TaskStatus.Error;
                        PostMessage(MessageType.Error, Format(e));
                    }
                }
                else Status = TaskStatus.Error;
                
                _taskDto.EndTime = DateTime.UtcNow;
            }
            
            PostMessage(MessageType.Exit, $"Exit with code: {_process?.ExitCode}");
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
        {
            lock(_messages) 
            {
                _messages.Enqueue(new TaskMessageDto(Id, DateTime.UtcNow, type, message));
            }
            
            
            try {
                _logFile.Write($"[{type}] ");
                _logFile.Write(message);
                _logFile.WriteLine();
            } catch(Exception) { }
        }

        public void Dispose()
        {
            Cancel();
            try { _process?.Dispose(); } catch(Exception) { }
            try { WorkingFolder.DeleteFolder(); } catch (Exception) { }
            try { _logFile.Dispose(); } catch (Exception) { }
        }

        private static StreamWriter CreateLogFile(string workingFolder, Guid id)
        {
            var logFolder = Path.Combine(workingFolder, "logs").CreateFolder();
            return new StreamWriter(new FileStream(Path.Combine(logFolder, $"{id:N}.log"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite));
        }

        private static string Format(Exception e)
        {
            var sb = new StringBuilder();
            while(e != null)
            {
                sb.Append("[Message] ");
                sb.Append(e.Message);
                sb.AppendLine();
                sb.Append("[Source] ");
                sb.Append(e.Source);
                sb.AppendLine();
                sb.Append("[StackTrace] ");
                sb.Append(e.StackTrace);
                sb.AppendLine();
                
                sb.AppendLine();
                e = e.InnerException;
            }

            return sb.ToString();
        }
    }
}
