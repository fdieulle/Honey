using Domain.Dtos.Workflows;
using System;

namespace Domain.Entities.Workflows
{
    public abstract class JobEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public JobStatus Status { get; set; }
    }
}
