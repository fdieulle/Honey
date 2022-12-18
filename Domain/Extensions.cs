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
                case TaskStatus.EndedWithoutSupervision:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsFinal(this TaskDto task) => task.Status.IsFinal();

        public static bool IsFinal(this QueuedTaskStatus status)
        {
            switch (status)
            {
                case QueuedTaskStatus.Pending:
                case QueuedTaskStatus.Running:
                case QueuedTaskStatus.CancelRequested:
                case QueuedTaskStatus.CancelPending:
                    return false;
                case QueuedTaskStatus.Completed:
                case QueuedTaskStatus.Cancel:
                case QueuedTaskStatus.Error:
                case QueuedTaskStatus.Deleted:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsFinal(this QueuedTaskDto dto) => dto.Status.IsFinal();

        public static bool CanCancel(this QueuedTaskStatus status)
        {
            switch (status)
            {
                case QueuedTaskStatus.Pending:
                case QueuedTaskStatus.Running:
                    return true;
                case QueuedTaskStatus.CancelRequested:
                case QueuedTaskStatus.CancelPending:
                case QueuedTaskStatus.Completed:
                case QueuedTaskStatus.Cancel:
                case QueuedTaskStatus.Error:
                case QueuedTaskStatus.Deleted:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool CanCancel(this QueuedTaskDto dto) => dto.Status.CanCancel();
    }
}
