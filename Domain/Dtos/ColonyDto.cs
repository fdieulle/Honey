using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class ColonyDto
    {
        [Required]
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public IEnumerable<string> Bees { get; set; }

        public ColonyDto Clone()
        {
            var clone = (ColonyDto)MemberwiseClone();
            if (Bees != null)
                clone.Bees = new List<string>(Bees);
            return clone;
        }
    }
}
