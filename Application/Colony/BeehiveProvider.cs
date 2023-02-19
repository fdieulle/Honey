using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Colony
{
    public class BeehiveProvider : IBeehiveProvider
    {
        private readonly Dictionary<string, Beehive> _beehives = new Dictionary<string, Beehive>();
        private readonly BeeKeeper _beeKeeper;
        private readonly IDispatcherFactory _dispatcherFactory;
        private readonly IColonyDb _database;
        private readonly TaskTracker _tracker;

        public BeehiveProvider(BeeKeeper beeKeeper, IDispatcherFactory dispatcherFactory, IColonyDb database, TaskTracker tracker)
        {
            _beeKeeper = beeKeeper;
            _dispatcherFactory = dispatcherFactory;
            _database = database;
            _tracker = tracker;
            var beehives = _database.FetchBeehives() ?? Enumerable.Empty<BeehiveDto>();
            foreach (var beehive in beehives)
                CreateBeehive(beehive, false);
        }

        public Beehive GetBeehive(string name)
        {
            lock(_beehives)
            {
                return _beehives.TryGetValue(name, out var beehive) ? beehive : null;
            }
        }

        public bool CreateBeehive(BeehiveDto dto)
        {
            lock (_beehives)
            {
                return CreateBeehive(dto, true);
            }
        }

        private bool CreateBeehive(BeehiveDto dto, bool withDb)
        {
            if (_beehives.ContainsKey(dto.Name)) return false;

            _beehives.Add(dto.Name, new Beehive(dto, _beeKeeper, _dispatcherFactory, _database, _tracker));
            if (withDb)
                _database.CreateBeehive(dto);
            return true;
        }

        public bool DeleteBeehive(string name)
        {
            lock (_beehives)
            {
                if (!_beehives.TryGetValue(name, out var beehive))
                    return false;
                
                _beehives.Remove(name);
                _database.DeleteBeehive(name);
                return true;
            }
        }

        public IEnumerable<BeehiveDto> GetBeehives()
        {
            lock (_beehives)
            {
                return _beehives.Values.Select(p => p.Dto).ToList();
            }
        }

        public bool UpdateBeehive(BeehiveDto dto)
        {
            lock (_beehives)
            {
                if (!_beehives.TryGetValue(dto.Name, out var beehive))
                    return false;

                beehive.Update(dto);
                _database.UpdateBeehive(dto);
                return true;
            }
        }

        public void Refresh()
        {
            lock (_beehives)
            {
                foreach (var beehive in _beehives.Values)
                    beehive.Refresh();
            }
        }
    }
}
