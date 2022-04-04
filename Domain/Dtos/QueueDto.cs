using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class QueueDto
    {
        [Required]
        public string Name { get; set; }

        public int MaxParallelTasks { get; set; }

        public IEnumerable<string> Ninjas { get; set; }

        public QueueDto Clone()
        {
            var clone = (QueueDto)MemberwiseClone();
            if (Ninjas != null)
                clone.Ninjas = new List<string>(Ninjas);
            return clone;
        }
    }
}
