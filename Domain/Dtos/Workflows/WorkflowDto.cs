using System;

namespace Domain.Dtos.Workflows
{
    public class WorkflowDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Beehive { get; set; }

        public Guid RootJobId { get; set; }
    }

    public class WorkflowParameters
    {
        public string Name { get; set; }
        public string Beehive { get; set; }
        public JobParameters RootJob { get; set; }
    }
}
