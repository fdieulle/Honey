using Hardware.Info;
using System;
using System.Linq;
using Application.Ninja;

namespace Infrastructure.Ninja
{
    internal class NinjaResourcesProvider : INinjaResourcesProvider
    {
        private static readonly IHardwareInfo hardwareInfo = new HardwareInfo(useAsteriskInWMI: false);

        public ResourcesDetails GetDiskSpace(string drive)
        {
            hardwareInfo.RefreshDriveList();

            var disk = hardwareInfo.DriveList
                .SelectMany(p => p.PartitionList)
                .SelectMany(p => p.VolumeList)
                .FirstOrDefault(p => p.Name == drive);

            return new ResourcesDetails
            {
                Total = disk.Size,
                Free = disk.FreeSpace
            };
        }

        public string GetMachineName() => Environment.MachineName;

        public string GetOSPlatform() => Environment.OSVersion.Platform.ToString();

        public string GetOSVersion() => Environment.OSVersion.Version.ToString();

        public ResourcesDetails GetPhysicalMemory()
        {
            hardwareInfo.RefreshMemoryStatus();

            return new ResourcesDetails
            {
                Total = hardwareInfo.MemoryStatus.TotalPhysical,
                Free = hardwareInfo.MemoryStatus.AvailablePhysical
            };
        }
    }
}
