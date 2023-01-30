using Domain.Dtos.Workflows;
using System.Linq;

namespace Application.Beehive.Workflows
{
    public static class Extensions
    {
        public static bool IsFinalStatus(this IJob job) => job.Status.IsFinal();
        public static bool CanStart(this IJob job) => job.Status.CanStart();
        public static bool CanCancel(this IJob job) 
            => job is ManyJobs many 
                ? many.Jobs.Any(p => p.CanCancel()) 
                : job.Status.CanCancel();
        public static bool CanRecover(this IJob job)
            => job is ManyJobs many
                ? many.Jobs.Any(p => p.CanRecover())
                : job.Status.CanRecover();
        public static bool CanDelete(this IJob job)
            => job is ManyJobs many
                ? many.Jobs.Any(p => p.CanDelete())
                : job.Status.CanDelete();
    }
}
