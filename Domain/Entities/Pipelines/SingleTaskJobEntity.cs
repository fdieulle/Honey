using System;

namespace Domain.Entities.Pipelines
{
    public class SingleTaskJobEntity : JobEntity
    {
        public Guid TaskId { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }
        public int NbCores { get; set; } = 1;
    }
}
