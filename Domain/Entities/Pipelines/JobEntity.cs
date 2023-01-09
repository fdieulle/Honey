using Domain.Dtos.Pipelines;
using System;

namespace Domain.Entities.Pipelines
{
    public abstract class JobEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public JobStatus Status { get; set; }
    }
}
