using System.Collections.Generic;

namespace Domain.Entities
{
    // Todo: Add a technical id on the queue
    public class QueueEntity
    {
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public string Bees { get; set; }
    }
}
