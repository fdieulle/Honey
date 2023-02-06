using System;

namespace Domain.Entities.Workflows
{
    public class WorkflowEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Colony { get; set; }

        public Guid RootJobId { get; set; }
    }
}
