using Domain.Dtos.Workflows;

namespace Application.Dojo.Workflows
{
    public static class Extensions
    {
        public static bool IsFinalStatus(this IJob job) => job.Status.IsFinal();
        public static bool CanStart(this IJob job) => job.Status.CanStart();
        public static bool CanCancel(this IJob job) => job.Status.CanCancel();
        public static bool CanRecover(this IJob job) => job.Status.CanRecover();
        public static bool CanDelete(this IJob job) => job.Status.CanDelete();
    }
}
