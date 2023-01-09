using Domain.Dtos.Pipelines;
using System;

namespace Domain.Entities.Pipelines
{
    public class LinkedJobsEntity : JobEntity
    {
        public LinkedJobType LinkType { get; set; }
        public Guid JobAId { get; set; }
        public Guid JobBId { get; set; }
    }
}
