using System;
using System.Collections.Generic;
using System.Text;

namespace Yumi.Domain.Dto
{
    public class NinjaDto
    {
        public string OS { get; set; }

        public string Address { get; set; }

        public bool IsUp { get; set; }

        public double PercentFreeCores { get; set; }

        public double PercentFreeMemory { get; set; }
        
        public double PercentFreeDiskSpace { get; set; }
    }
}
