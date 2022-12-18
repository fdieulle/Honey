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
    }
}
