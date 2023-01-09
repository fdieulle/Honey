using System;

namespace Domain.Entities.Pipelines
{
    public class PipelineEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string QueueName { get; set; }

        public Guid RootJobId { get; set; }
    }
}
