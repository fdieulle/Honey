using Application;

namespace Infrastructure.Dojo
{
    internal class NinjaProxyFactory : INinjaFactory
    {
        public INinja Create(string address) => new NinjaProxy(address);
    }
}
