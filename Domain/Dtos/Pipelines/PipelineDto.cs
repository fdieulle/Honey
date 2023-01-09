using System;

namespace Domain.Dtos.Pipelines
{
    public class PipelineDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string QueueName { get; set; }

        public Guid RootJobId { get; set; }
    }

    public class PipelineParameters
    {
        public string Name { get; set; }
        public string QueueName { get; set; }
        public JobParameters RootJob { get; set; }
    }
}
