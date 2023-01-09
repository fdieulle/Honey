using System;

namespace Domain.Dtos.Pipelines
{
    public class SingleTaskJobDto : JobDto
    {
        public Guid TaskId { get; set; }

        public TaskParameters StartTask { get; set; }
    }

    public class SingleTaskJobParameters : JobParameters
    {
        public TaskParameters StartTask { get; set; }
    }
}
