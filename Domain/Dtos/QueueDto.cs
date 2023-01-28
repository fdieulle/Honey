using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class QueueDto
    {
        [Required]
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public IEnumerable<string> Bees { get; set; }

        public QueueDto Clone()
        {
            var clone = (QueueDto)MemberwiseClone();
            if (Bees != null)
                clone.Bees = new List<string>(Bees);
            return clone;
        }
    }
}
