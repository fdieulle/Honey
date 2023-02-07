using System.Collections.Generic;

namespace Domain.Entities
{
    // Todo: Add a technical id on the beehive
    public class BeehiveEntity
    {
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public string Bees { get; set; }
    }
}
