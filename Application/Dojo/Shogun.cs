using Domain.Dtos;
using System;
using System.Collections.Generic;

namespace Application.Dojo
{
    public class Shogun : IShogun
    {
        private readonly QueueProvider _queueProvider;
        private readonly Dictionary<Guid, string> _tasksByQueue = new Dictionary<Guid, string>();

        public Shogun(QueueProvider queueProvider)
        {
            _queueProvider = queueProvider;
        }

        public Guid Execute(string queueName, StartTaskDto task)
        {
            var queue = _queueProvider.GetQueue(queueName);
            var id = queue.StartTask(task);
            _tasksByQueue[id] = queueName;
            return id;
        }

        public void Cancel(Guid id)
        {
            if (!_tasksByQueue.TryGetValue(id, out var queueName))
                return;

            var queue = _queueProvider.GetQueue(queueName);
            queue.CancelTask(id);
        }
    } 
}
