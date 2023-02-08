namespace Domain.Dtos.Workflows
{
    public static class Extensions
    {
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

        public static bool CanCancel(this JobStatus status)
            => !status.IsFinal() && status != JobStatus.CancelRequested;

        public static bool CanRecover(this JobStatus status)
            => status.IsFinal() && status != JobStatus.Completed && !status.IsOrWillBeDeleted();

        public static bool CanDelete(this JobStatus status)
            => (status == JobStatus.Pending || status.IsFinal()) && !status.IsOrWillBeDeleted();

        private static bool IsOrWillBeDeleted(this JobStatus status) 
            => status == JobStatus.DeleteRequested || status == JobStatus.Deleted;
    }
}
