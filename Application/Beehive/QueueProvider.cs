using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive
{
    public class QueueProvider : IQueueProvider
    {
        private readonly Dictionary<string, Queue> _queues = new Dictionary<string, Queue>();
        private readonly BeeKeeper _beeKeeper;
        private readonly IBeehiveDb _database;
        private readonly TaskTracker _tracker;

        public QueueProvider(BeeKeeper beeKeeper, IBeehiveDb database, TaskTracker tracker)
        {
            _beeKeeper = beeKeeper;
            _database = database;
            _tracker = tracker;
            var queues = _database.FetchQueues() ?? Enumerable.Empty<QueueDto>();
            foreach (var queue in queues)
                CreateQueue(queue, false);
        }

        public Queue GetQueue(string name)
        {
            lock(_queues)
            {
                return _queues.TryGetValue(name, out var queue) ? queue : null;
            }
        }

        public bool CreateQueue(QueueDto dto)
        {
            lock (_queues)
            {
                return CreateQueue(dto, true);
            }
        }

        private bool CreateQueue(QueueDto dto, bool withDb)
        {
            if (_queues.ContainsKey(dto.Name)) return false;

            _queues.Add(dto.Name, new Queue(dto, _beeKeeper, _database, _tracker));
            if (withDb)
                _database.CreateQueue(dto);
            return true;
        }

        public bool DeleteQueue(string name)
        {
            lock (_queues)
            {
                if (!_queues.TryGetValue(name, out var queue))
                    return false;
                
                _queues.Remove(name);
                _database.DeleteQueue(name);
                return true;
            }
        }

        public IEnumerable<QueueDto> GetQueues()
        {
            lock (_queues)
            {
                return _queues.Values.Select(p => p.Dto).ToList();
            }
        }

        public bool UpdateQueue(QueueDto dto)
        {
            lock (_queues)
            {
                if (!_queues.TryGetValue(dto.Name, out var queue))
                    return false;

                queue.Update(dto);
                _database.UpdateQueue(dto);
                return true;
            }
        }

        public void Refresh()
        {
            lock (_queues)
            {
                foreach (var queue in _queues.Values)
                    queue.Refresh();
            }
        }
    }
}
