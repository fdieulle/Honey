using AntDesign;
using Domain.Dtos.Workflows;
using Domain.ViewModels;

namespace Beehive
{
    public static class Extensions
    {
        public static string ToCssClass(this JobStatus status)
        {
            switch (status)
            {
                case JobStatus.Pending:
                    return "job-pending";
                case JobStatus.Running:
                    return "job-running";
                case JobStatus.Completed:
                    return "job-completed";
                case JobStatus.CancelRequested:
                case JobStatus.Cancel:
                    return "job-cancel";
                case JobStatus.Error:
                    return "job-error";
                case JobStatus.DeleteRequested:
                case JobStatus.Deleted:
                    return "job-deleted";
                default:
                    return string.Empty;
            }
        }

        public static string ToCssClass(this JobViewModel vm) => vm.Status.ToCssClass();

        public static string ToCssClass(this WorkflowViewModel vm) => vm.Status.ToCssClass();

        public static ProgressStatus ToProgressStatus(this JobStatus status)
        {
            switch (status)
            {
                case JobStatus.Pending:
                case JobStatus.DeleteRequested:
                case JobStatus.Deleted:
                default:
                    return ProgressStatus.Normal;
                case JobStatus.Running:
                case JobStatus.CancelRequested:
                    return ProgressStatus.Active;
                case JobStatus.Completed:
                case JobStatus.Cancel:
                    return ProgressStatus.Success;
                case JobStatus.Error:
                    return ProgressStatus.Exception;
            }
        }

        public static ProgressStatus ToProgressStatus(this JobViewModel vm) => vm.Status.ToProgressStatus();

        public static ProgressStatus ToProgressStatus(this WorkflowViewModel vm) => vm.Status.ToProgressStatus();
    }
}
