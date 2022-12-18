using System;
using Domain.Dtos;

namespace Domain
{
    public static class Extensions
    {
        public static bool IsFinal(this TaskStatus status)
        {
            switch (status)
            {
                case TaskStatus.Pending:
                case TaskStatus.Running:
                    return false;
                case TaskStatus.Done:
                case TaskStatus.Cancel:
                case TaskStatus.Error:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsFinal(this TaskDto task) => task.Status.IsFinal();

        public static bool IsFinal(this RemoteTaskStatus status)
        {
            switch (status)
            {
                case RemoteTaskStatus.Pending:
                case RemoteTaskStatus.Running:
                case RemoteTaskStatus.CancelRequested:
                case RemoteTaskStatus.CancelPending:
                    return false;
                case RemoteTaskStatus.Completed:
                case RemoteTaskStatus.Cancel:
                case RemoteTaskStatus.Error:
                case RemoteTaskStatus.Deleted:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsFinal(this RemoteTaskDto dto) => dto.Status.IsFinal();

        public static bool CanCancel(this RemoteTaskStatus status)
        {
            switch (status)
            {
                case RemoteTaskStatus.Pending:
                case RemoteTaskStatus.Running:
                    return true;
                case RemoteTaskStatus.CancelRequested:
                case RemoteTaskStatus.CancelPending:
                case RemoteTaskStatus.Completed:
                case RemoteTaskStatus.Cancel:
                case RemoteTaskStatus.Error:
                case RemoteTaskStatus.Deleted:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool CanCancel(this RemoteTaskDto dto) => dto.Status.CanCancel();
    }
}
