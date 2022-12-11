using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Dojo : IDojo
    {
        private static readonly Comparer<NinjaDto> heuristic = Comparer<NinjaDto>.Create((x, y) =>
        {
            var compare = x.PercentFreeCores.CompareTo(y.PercentFreeCores);
            if (compare != 0) return compare;

            return x.PercentFreeMemory.CompareTo(y.PercentFreeMemory);
        });

        private readonly INinjaFactory _factory;
        private readonly IDojoDb _database;
        private Dictionary<string, Ninja> _ninjas = new Dictionary<string, Ninja>();

        public INinjaFactory Container => _factory;

        public IEnumerable<Ninja> Ninjas => _ninjas.Values;

        public Dojo(INinjaFactory factory, IDojoDb database)
        {
            _factory = factory;
            _database = database;

            var ninjas = _database.FetchNinjas() ?? Enumerable.Empty<NinjaDto>();
            foreach (var ninja in ninjas)
                EnrollNinja(ninja.Address, false);
        }

        public IEnumerable<NinjaDto> GetNinjas()
        {
            lock (_ninjas)
            {
                return _ninjas.Values.Select(p => p.Dto).ToList();
            }
        }

        public void EnrollNinja(string address)
        {
            lock (_ninjas)
            {
                EnrollNinja(address, true);
            }
        }

        private void EnrollNinja(string address, bool withDb)
        {
            if (_ninjas.ContainsKey(address))
                return;

            var ninja = new Ninja(address, _factory.Create(address));
            _ninjas.Add(address, ninja);

            if (withDb)
                _database.CreateNinja(ninja.Dto);
        }

        public void RevokeNinja(string address)
        {
            lock (_ninjas)
            {
                if (!_ninjas.TryGetValue(address, out var ninja))
                    return;

                _ninjas.Remove(address);
                _database.DeleteNinja(address);
            }
        }

        public void Refresh()
        {
            List<Ninja> ninjas;
            lock (_ninjas)
            {
                ninjas = _ninjas.Values.ToList();
            }
            
            foreach (var ninja in ninjas)
                ninja.Refresh();
        }

        public Ninja GetNextNinja(HashSet<string> ninjas = null)
        {
            lock (_ninjas)
            {
                var selectedNinjas = _ninjas.Values.Where(p => p.Dto.IsUp);
                if (ninjas != null && ninjas.Count > 0)
                    selectedNinjas = selectedNinjas.Where(p => ninjas.Contains(p.Address));

                return selectedNinjas
                    .OrderByDescending(p => p.Dto, heuristic)
                    .Where(p => p.Dto.PercentFreeCores > 0)
                    .FirstOrDefault();
            }
        }

        public Ninja GetNinja(string address)
        {
            lock (_ninjas)
            {
                return _ninjas.TryGetValue(address, out var ninja) ? ninja : null;
            }
        }
    }
}
