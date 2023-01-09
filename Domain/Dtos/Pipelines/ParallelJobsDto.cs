using System;

namespace Domain.Dtos.Pipelines
{
    public class ParallelJobsDto : JobDto
    {
        public Guid[] JobIds { get; set; } = Array.Empty<Guid>();
    }

    public class ParallelJobsParameters : JobParameters
    {
        public JobParameters[] Jobs { get; set; } = Array.Empty<JobParameters>();
    }
}
