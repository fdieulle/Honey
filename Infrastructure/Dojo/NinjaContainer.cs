using Application;
using System;
using System.Collections.Generic;

namespace Infrastructure.Dojo
{
    internal class NinjaContainer : INinjaContainer, IDisposable
    {
        private readonly Dictionary<string, INinja> _proxies = new Dictionary<string, INinja>();

        public INinja Resolve(string address)
        {
            if (!_proxies.TryGetValue(address, out var ninja))
                _proxies.Add(address, ninja = new NinjaProxy(address));
            return ninja;
        }

        public void Dispose()
        {
            foreach (var pair in _proxies)
            {
                if (pair.Value is IDisposable disposable)
                    disposable.Dispose();
            }

            _proxies.Clear();
        }
    }
}
