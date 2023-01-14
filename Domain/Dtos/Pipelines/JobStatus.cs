namespace Domain.Dtos.Pipelines
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
