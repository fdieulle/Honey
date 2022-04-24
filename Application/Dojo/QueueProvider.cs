using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class QueueProvider : IQueueProvider
    {
        private readonly Dictionary<string, Queue> _queues = new Dictionary<string, Queue>();
        private readonly Dojo _dojo;
        private readonly IDojoDb _database;

        public QueueProvider(Dojo dojo, IDojoDb database)
        {
            _dojo = dojo;
            _database = database;

            var queues = _database.FetchQueues() ?? Enumerable.Empty<QueueDto>();
            foreach (var queue in queues)
                CreateQueue(queue, false);
        }

        public Queue GetQueue(string name) => _queues.TryGetValue(name, out var queue) ? queue : null;

        public bool CreateQueue(QueueDto dto) => CreateQueue(dto, true);

        private bool CreateQueue(QueueDto dto, bool withDb)
        {
            if (_queues.ContainsKey(dto.Name)) return false;

            _queues.Add(dto.Name, new Queue(dto, _dojo, _database));
            if (withDb)
                _database.CreateQueue(dto);
            return true;
        }

        public bool DeleteQueue(string name)
        {
            var result = _queues.Remove(name);
            _database.DeleteQueue(name);
            return result;
        }
        
        public IEnumerable<QueueDto> GetQueues() => _queues.Values.Select(p => p.Dto).ToList();

        public bool UpdateQueue(QueueDto dto)
        {
            if (!_queues.TryGetValue(dto.Name, out var queue))
                return false;

            queue.Update(dto);
            _database.UpdateQueue(dto);
            return true;
        }
    }
}
