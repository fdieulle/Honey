using Domain.Dtos;

namespace Application.Bee
{
    public interface IBeeResourcesProvider
    {
        string GetMachineName();

        string GetBaseUri();

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
