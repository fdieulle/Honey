using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Application.Dojo
{
    public class Dojo : IDojo, IDisposable
    {
        private const int WATCH_DOG_PERIOD = 3000;

        private static readonly Comparer<NinjaDto> heuristic = Comparer<NinjaDto>.Create((x, y) =>
        {
            var compare = x.PercentFreeCores.CompareTo(y.PercentFreeCores);
            if (compare != 0) return compare;

            return x.PercentFreeMemory.CompareTo(y.PercentFreeMemory);
        });

        private readonly INinjaContainer _ninjaContainer;
        private Dictionary<string, Ninja> _ninjas = new Dictionary<string, Ninja>();
        private readonly Timer _timer;
        private bool _isDisposed;

        public event Action Updated;

        public INinjaContainer Container => _ninjaContainer;

        public IEnumerable<Ninja> Ninjas => _ninjas.Values;

        public Dojo(INinjaContainer ninjaContainer)
        {
            _timer = new Timer(OnWatchDogWalk, null, WATCH_DOG_PERIOD, Timeout.Infinite);
            _ninjaContainer = ninjaContainer;
        }

        public IEnumerable<NinjaDto> GetNinjas()
        {
            return _ninjas.Values.Select(p => p.Dto).ToList();
        }

        public void EnrollNinja(string address)
        {
            if (_ninjas.ContainsKey(address))
                return;

            var ninja = new Ninja(address, _ninjaContainer.Resolve(address));
            _ninjas.Add(address, ninja);
        }

        public void RevokeNinja(string address)
        {
            if (!_ninjas.TryGetValue(address, out var ninja))
                return;

            _ninjas.Remove(address);
        }

        private void OnWatchDogWalk(object state)
        {
            foreach (var ninja in _ninjas.Values)
                ninja.Refresh();

            Updated?.Invoke();

            if (!_isDisposed)
                _timer.Change(WATCH_DOG_PERIOD, Timeout.Infinite);
        }

        public Ninja GetNextNinja(HashSet<string> ninjas = null)
        {
            var selectedNinjas = _ninjas.Values.Where(p => p.Dto.IsUp);
            if (ninjas != null && ninjas.Count > 0)
                selectedNinjas = selectedNinjas.Where(p => ninjas.Contains(p.Address));

            return selectedNinjas
                .OrderByDescending(p => p.Dto, heuristic)
                .Where(p => p.Dto.PercentFreeCores > 0)
                .FirstOrDefault();
        }

        public Ninja GetNinja(string address) => 
            _ninjas.TryGetValue(address, out var ninja) ? ninja : null;

        public void Dispose()
        {
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
