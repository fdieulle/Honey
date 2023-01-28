using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Dojo : IDojo
    {
        private static readonly Comparer<BeeDto> heuristic = Comparer<BeeDto>.Create((x, y) =>
        {
            var compare = x.PercentFreeCores.CompareTo(y.PercentFreeCores);
            if (compare != 0) return compare;

            return x.PercentFreeMemory.CompareTo(y.PercentFreeMemory);
        });

        private readonly IBeeFactory _factory;
        private readonly IDojoDb _database;
        private Dictionary<string, Bee> _bees = new Dictionary<string, Bee>();

        public IBeeFactory Container => _factory;

        public IEnumerable<Bee> Bees => _bees.Values;

        public Dojo(IBeeFactory factory, IDojoDb database)
        {
            _factory = factory;
            _database = database;

            var bees = _database.FetchBees() ?? Enumerable.Empty<BeeDto>();
            foreach (var bee in bees)
                EnrollBee(bee.Address, false);
        }

        public IEnumerable<BeeDto> GetBees()
        {
            lock (_bees)
            {
                return _bees.Values.Select(p => p.Dto).ToList();
            }
        }

        public void EnrollBee(string address)
        {
            lock (_bees)
            {
                EnrollBee(address, true);
            }
        }

        private void EnrollBee(string address, bool withDb)
        {
            if (_bees.ContainsKey(address))
                return;

            var bee = new Bee(address, _factory.Create(address));
            _bees.Add(address, bee);

            if (withDb)
                _database.CreateBee(bee.Dto);
        }

        public void RevokeBee(string address)
        {
            lock (_bees)
            {
                if (!_bees.TryGetValue(address, out var bee))
                    return;

                _bees.Remove(address);
                _database.DeleteBee(address);
            }
        }

        public void Refresh()
        {
            List<Bee> bees;
            lock (_bees)
            {
                bees = _bees.Values.ToList();
            }
            
            foreach (var bee in bees)
                bee.Refresh();
        }

        public Bee GetNextBee(HashSet<string> bees = null)
        {
            lock (_bees)
            {
                var selectedBees = _bees.Values.Where(p => p.Dto.IsUp);
                if (bees != null && bees.Count > 0)
                    selectedBees = selectedBees.Where(p => bees.Contains(p.Address));

                return selectedBees
                    .OrderByDescending(p => p.Dto, heuristic)
                    .Where(p => p.Dto.PercentFreeCores > 0)
                    .FirstOrDefault();
            }
        }

        public Bee GetBee(string address)
        {
            lock (_bees)
            {
                return _bees.TryGetValue(address, out var bee) ? bee : null;
            }
        }
    }
}
