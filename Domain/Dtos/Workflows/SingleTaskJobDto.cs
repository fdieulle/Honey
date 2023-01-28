using System;

namespace Domain.Dtos.Workflows
{
    public class SingleTaskJobDto : JobDto
    {
        public Guid TaskId { get; set; }

        public TaskParameters Parameters { get; set; }
    }

    public class SingleTaskJobParameters : JobParameters
    {
        public TaskParameters Task { get; set; }
    }
}
