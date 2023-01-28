namespace Domain.Dtos
{
    public class BeeResourcesDto
    {
        public string MachineName { get; set; }

        public string OSPlatform { get; set; }

        public string OSVersion { get; set; }

        public int NbCores { get; set; }

        public int NbFreeCores { get; set; }

        public ulong DiskSpace { get; set; }

        public ulong DiskFreeSpace { get; set; }

        public ulong TotalPhysicalMemory { get; set; }

        public ulong AvailablePhysicalMemory { get; set; }
    }
}
