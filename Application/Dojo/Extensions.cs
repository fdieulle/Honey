using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

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

        public static bool IsFinal(this QueuedTaskStatus status)
        {
            switch (status)
            {
                case QueuedTaskStatus.Pending:
                case QueuedTaskStatus.Running:
                case QueuedTaskStatus.CancelRequested:
                case QueuedTaskStatus.CancelPending:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsFinalStatus(this RemoteTaskDto dto) => dto.Status.IsFinal();
    }
}
