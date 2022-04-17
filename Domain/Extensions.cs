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
    }
}
