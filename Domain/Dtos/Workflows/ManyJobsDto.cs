using System;

namespace Domain.Dtos.Workflows
{
    public enum JobsBehavior
    {
        Parallel,
        Sequential
    }
    public class ManyJobsDto : JobDto
    {
        public JobsBehavior Behavior { get; set; }
        public Guid[] JobIds { get; set; } = Array.Empty<Guid>();

        public override string ToString() => $"{base.ToString()} - {Behavior}";
    }

    public class ManyJobsParameters : JobParameters
    {
        public JobsBehavior Behavior { get; set; }
        public JobParameters[] Jobs { get; set; } = Array.Empty<JobParameters>();

        public override string ToString() => $"{base.ToString()} - {Behavior}";
    }
}
