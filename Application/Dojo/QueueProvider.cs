using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class QueueProvider : IQueueProvider
    {
        private readonly Dictionary<string, Queue> _queues = new Dictionary<string, Queue>();
        private readonly Dojo _dojo;

        public QueueProvider(Dojo dojo)
        {
            _dojo = dojo;
        }

        public Queue GetQueue(string name) => _queues.TryGetValue(name, out var queue) ? queue : null;

        public bool CreateQueue(QueueDto dto)
        {
            if (_queues.ContainsKey(dto.Name)) return false;

            _queues.Add(dto.Name, new Queue(dto, _dojo));
            return true;
        }

        public bool DeleteQueue(string name) => _queues.Remove(name);

        public IEnumerable<QueueDto> GetQueues() => _queues.Values.Select(p => p.Dto).ToList();

        public bool UpdateQueue(QueueDto dto)
        {
            if (!_queues.TryGetValue(dto.Name, out var queue))
                return false;

            queue.Update(dto);
            return true;
        }
    }
}
