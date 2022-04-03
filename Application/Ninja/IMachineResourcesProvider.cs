using Domain.Dtos;

namespace Application.Ninja
{
    public interface INinjaResourcesProvider
    {
        string GetMachineName();

        string GetOSPlatform();

        string GetOSVersion();

        ResourcesDetails GetPhysicalMemory();

        ResourcesDetails GetDiskSpace(string drive);
    }

    public struct ResourcesDetails
    {
        public ulong Total { get; set; }

        public ulong Free { get; set; }
    }
}
