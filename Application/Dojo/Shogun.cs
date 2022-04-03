using Domain.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Shogun
    {
        private readonly Dictionary<string, Queue> _queues = new Dictionary<string, Queue>();
        private readonly Dojo _dojo;

        public Shogun(Dojo dojo)
        {
            _dojo = dojo;
        }

        public IEnumerable<QueueDto> Queues => _queues.Values.Select(p => p.Dto);

        public void AddQueue(string name, int maxParallelTasks = -1, IEnumerable<string> ninjas = null)
        {
            if (!_queues.TryGetValue(name, out var queue))
                _queues.Add(name, queue = new Queue(name, _dojo));
            queue.Update(maxParallelTasks, ninjas);
        }

        public void RemoveQueue(string name)
        {
            _queues.Remove(name);
        }

        public Queue GetQueue(string name)
        {
            return _queues.TryGetValue(name, out var shogoun) ? shogoun : null;
        }
    }
}
