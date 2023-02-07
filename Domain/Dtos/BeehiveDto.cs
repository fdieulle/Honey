using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class BeehiveDto
    {
        [Required]
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public IEnumerable<string> Bees { get; set; }

        public BeehiveDto Clone()
        {
            var clone = (BeehiveDto)MemberwiseClone();
            if (Bees != null)
                clone.Bees = new List<string>(Bees);
            return clone;
        }
    }
}
