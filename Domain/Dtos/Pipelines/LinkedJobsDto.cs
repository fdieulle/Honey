using System;

namespace Domain.Dtos.Pipelines
{
    public class LinkedJobsDto : JobDto
    {
        public LinkedJobType LinkType { get; set; }
        public Guid JobAId { get; set; }
        public Guid JobBId { get; set; }
    }

    public class LinkedJobsParameters : JobParameters
    {
        public LinkedJobType LinkType { get; set; }
        public JobParameters JobA { get; set; }
        public JobParameters JobB { get; set; }
    }
}
