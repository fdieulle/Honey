using Application;

namespace Infrastructure.Dojo
{
    internal class BeeProxyFactory : IBeeFactory
    {
        public IBee Create(string address) => new BeeProxy(address);
    }
}
