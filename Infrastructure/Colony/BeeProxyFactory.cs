using Application;

namespace Infrastructure.Colony
{
    internal class BeeProxyFactory : IBeeFactory
    {
        public IBee Create(string address) => new BeeProxy(address);
    }
}
