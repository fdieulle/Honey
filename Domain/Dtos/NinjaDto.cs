using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class NinjaDto
    {
        public string OS { get; set; }

        [Required]
        [Url]
        public string Address { get; set; }

        public bool IsUp { get; set; }

        public double PercentFreeCores { get; set; }

        public double PercentFreeMemory { get; set; }

        public double PercentFreeDiskSpace { get; set; }
    }
}
