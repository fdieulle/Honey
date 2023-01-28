using Application;

namespace Infrastructure.Beehive
{
    internal class BeeProxyFactory : IBeeFactory
    {
        public IBee Create(string address) => new BeeProxy(address);
    }
}
