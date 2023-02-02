using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive
{
    public class Beehive : IBeehive
    {
        private static readonly Comparer<BeeDto> heuristic = Comparer<BeeDto>.Create((x, y) =>
        {
            var compare = x.PercentFreeCores.CompareTo(y.PercentFreeCores);
            if (compare != 0) return compare;

            return x.PercentFreeMemory.CompareTo(y.PercentFreeMemory);
        });

        private readonly IBeeFactory _factory;
        private readonly IBeehiveDb _database;
        private Dictionary<string, Bee> _bees = new Dictionary<string, Bee>();

        public IBeeFactory Container => _factory;

        public IEnumerable<Bee> Bees => _bees.Values;

        public Beehive(IBeeFactory factory, IBeehiveDb database)
        {
            _factory = factory;
            _database = database;

            var bees = _database.FetchBees() ?? Enumerable.Empty<BeeDto>();
            foreach (var bee in bees)
                EnrollBee(bee.Address, false);
        }

        public List<BeeDto> GetBees()
        {
            lock (_bees)
            {
                return _bees.Values.Select(p => p.Dto).ToList();
            }
        }

        public bool EnrollBee(string address)
        {
            lock (_bees)
            {
                return EnrollBee(address, true);
            }
        }

        private bool EnrollBee(string address, bool withDb)
        {
            if (_bees.ContainsKey(address))
                return false;

            var bee = new Bee(address, _factory.Create(address));
            _bees.Add(address, bee);

            if (withDb)
                _database.CreateBee(bee.Dto);

            return true;
        }

        public bool RevokeBee(string address)
        {
            lock (_bees)
            {
                if (!_bees.TryGetValue(address, out var bee))
                    return false;

                _bees.Remove(address);
                _database.DeleteBee(address);
                return true;
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
