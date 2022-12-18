using Application.Ninja;
using Domain.Dtos;
using Domain.Dtos.Sequences;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Dojo
{
    public class Shogun : IShogun
    {
        private readonly QueueProvider _queueProvider;
        private readonly Dictionary<Guid, string> _tasksByQueue = new Dictionary<Guid, string>();
        

        public Shogun(QueueProvider queueProvider)
        {
            _queueProvider = queueProvider;

            foreach (var queueName in _queueProvider.GetQueues().Select(p => p.Name))
            {
                var queue = _queueProvider.GetQueue(queueName);
                if (queue == null) continue;
                var allTasks = queue.GetAllTasks();
                if (allTasks == null) continue;
                foreach (var task in allTasks)
                    _tasksByQueue[task.Id] = queueName;
            }
        }

        public Guid Execute(string queueName, string name, StartTaskDto task)
        {
            var queue = _queueProvider.GetQueue(queueName);
            var id = queue.StartTask(name, task);
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

        public void DeleteTask(Guid id)
        {
            if (!_tasksByQueue.TryGetValue(id, out var queueName))
                return;

            var queue = _queueProvider.GetQueue(queueName);
            queue.DeleteTask(id);
        }

        public IEnumerable<RemoteTaskDto> GetAllTasks()
        {
            var result = new List<RemoteTaskDto>();
            foreach(var queueName in _queueProvider.GetQueues().Select(p => p.Name))
            {
                var queue = _queueProvider.GetQueue(queueName);
                if (queue == null) continue;
                var allTasks = queue.GetAllTasks();
                if (allTasks == null) continue;

                result.AddRange(allTasks);
            }

            return result;
        }
    } 
}
