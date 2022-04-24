using System.Collections.Generic;

namespace Domain.Entities
{
    public class QueueEntity
    {
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public string Ninjas { get; set; }
    }
}
