using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive
{
    public class ColonyProvider : IColonyProvider
    {
        private readonly Dictionary<string, Colony> _colonies = new Dictionary<string, Colony>();
        private readonly BeeKeeper _beeKeeper;
        private readonly IBeehiveDb _database;
        private readonly TaskTracker _tracker;

        public ColonyProvider(BeeKeeper beeKeeper, IBeehiveDb database, TaskTracker tracker)
        {
            _beeKeeper = beeKeeper;
            _database = database;
            _tracker = tracker;
            var colonies = _database.FetchColonies() ?? Enumerable.Empty<ColonyDto>();
            foreach (var colony in colonies)
                CreateColony(colony, false);
        }

        public Colony GetColony(string name)
        {
            lock(_colonies)
            {
                return _colonies.TryGetValue(name, out var colony) ? colony : null;
            }
        }

        public bool CreateColony(ColonyDto dto)
        {
            lock (_colonies)
            {
                return CreateColony(dto, true);
            }
        }

        private bool CreateColony(ColonyDto dto, bool withDb)
        {
            if (_colonies.ContainsKey(dto.Name)) return false;

            _colonies.Add(dto.Name, new Colony(dto, _beeKeeper, _database, _tracker));
            if (withDb)
                _database.CreateColony(dto);
            return true;
        }

        public bool DeleteColony(string name)
        {
            lock (_colonies)
            {
                if (!_colonies.TryGetValue(name, out var colony))
                    return false;
                
                _colonies.Remove(name);
                _database.DeleteColony(name);
                return true;
            }
        }

        public IEnumerable<ColonyDto> GetColonies()
        {
            lock (_colonies)
            {
                return _colonies.Values.Select(p => p.Dto).ToList();
            }
        }

        public bool UpdateColony(ColonyDto dto)
        {
            lock (_colonies)
            {
                if (!_colonies.TryGetValue(dto.Name, out var colony))
                    return false;

                colony.Update(dto);
                _database.UpdateColony(dto);
                return true;
            }
        }

        public void Refresh()
        {
            lock (_colonies)
            {
                foreach (var colony in _colonies.Values)
                    colony.Refresh();
            }
        }
    }
}
