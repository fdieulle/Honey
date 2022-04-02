using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Dojo.Services
{
    public class NinjaModel
    {
        public string OS { get; set; }

        [Required]
        [Url]
        [DisplayName("Name")]
        public string Address { get; set; }

        public bool IsUp { get; set; }

        [DisplayName("Cores %")]
        public double PercentFreeCores { get; set; }
        public string CoresColor => GetColor(PercentFreeCores);

        [DisplayName("RAM %")]
        public double PercentFreeMemory { get; set; }
        public string MemoryColor => GetColor(PercentFreeMemory);

        [DisplayName("Disk %")]
        public double PercentFreeDiskSpace { get; set; }
        public string DiskColor => GetColor(PercentFreeDiskSpace);

        private string GetColor(double percent)
        {
            if (percent >= 90) return "#dc3545";
            if (percent >= 75) return "#fd7e14";
            return "#28a745";
        }
    }
}
