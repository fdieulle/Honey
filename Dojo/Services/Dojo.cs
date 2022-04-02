using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Yumi.Application;

namespace Dojo.Services
{
    public class Dojo : IDisposable
    {
        private const int WATCH_DOG_PERIOD = 3000;

        private Dictionary<string, Ninja> _ninjas = new Dictionary<string, Ninja>();
        private readonly Timer _timer;
        private bool _isDisposed;

        private static readonly Comparer<NinjaModel> heuristic = Comparer<NinjaModel>.Create((x, y) =>
        {
            var compare = x.PercentFreeCores.CompareTo(y.PercentFreeCores);
            if (compare != 0) return compare;

            return x.PercentFreeMemory.CompareTo(y.PercentFreeMemory);
        });

        public event Action<NinjaModel> NinjaAdded;
        public event Action<NinjaModel> NinjaRemoved;
        public event Action Updated;

        public IEnumerable<Ninja> Ninjas => _ninjas.Values;

        public Dojo()
        {
            _timer = new Timer(OnWatchDogWalk, null, WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public void EnrollNinja(string address)
        {
            if (_ninjas.ContainsKey(address))
                return;

            var ninja = new Ninja(address, new NinjaProxy(address)); // TODO: us DI here for the proxy which should be part of the infrastructure
            _ninjas.Add(address, ninja);
            NinjaAdded?.Invoke(ninja.Model);
        }

        public void RevokeNinja(string address)
        {
            if (!_ninjas.TryGetValue(address, out var ninja))
                return;

            _ninjas.Remove(address);
            NinjaRemoved?.Invoke(ninja.Model);
        }

        private void OnWatchDogWalk(object state)
        {
            foreach (var ninja in _ninjas.Values)
                ninja.Refresh();

            Updated?.Invoke();

            if (!_isDisposed)
                _timer.Change(WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public INinja GetNextNinja()
        {
            return _ninjas.Values.OrderByDescending(p => p.Model, heuristic).FirstOrDefault();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
