using Application.Dojo.Workflows;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;

namespace Application.Dojo
{
    public static class Extensions
    {
        public static Guid StartTask(this INinja ninja, RemoteTaskDto task)
        {
            if (task == null) return Guid.Empty;
            var startTask = task.StartTask;
            if (startTask == null) return Guid.Empty;

            return ninja.StartTask(startTask.Command, startTask.Arguments, startTask.NbCores);
        }

        public static bool IsFinal(this RemoteTaskStatus status)
        {
            switch (status)
            {
                case RemoteTaskStatus.Pending:
                case RemoteTaskStatus.Running:
                case RemoteTaskStatus.CancelRequested:
                case RemoteTaskStatus.CancelPending:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsFinalStatus(this RemoteTaskDto dto) => dto.Status.IsFinal();

        public static bool IsFinal(this JobStatus status)
        {
            switch (status)
            {
                case JobStatus.Pending:
                case JobStatus.Running:
                case JobStatus.CancelRequested:
                    return false;
                default:
                    return true;
            }
        }

        public static bool CanStart(this JobStatus status) => status == JobStatus.Pending;
        public static bool CanStart(this IJob job) => job.Status.CanStart();
        public static bool CanCancel(this JobStatus status) => !status.IsFinal() && status != JobStatus.CancelRequested;
        public static bool CanCancel(this IJob job) => job.Status.CanCancel();

        public static bool CanDelete(this JobStatus status) => status.IsFinal() && status != JobStatus.Deleted;
        public static bool CanDelete(this IJob job) => job.Status.CanDelete();
    }
}
