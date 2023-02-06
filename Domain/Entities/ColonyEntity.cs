using System.Collections.Generic;

namespace Domain.Entities
{
    // Todo: Add a technical id on the colony
    public class ColonyEntity
    {
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public string Bees { get; set; }
    }
}
