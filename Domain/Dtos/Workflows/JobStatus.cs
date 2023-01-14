namespace Domain.Dtos.Workflows
{
    public enum JobStatus
    {
        Pending,
        Running,
        Completed,
        CancelRequested,
        Cancel,
        Error,
        DeleteRequested,
        Deleted
    }
}
