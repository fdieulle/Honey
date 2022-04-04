using Application;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Dojo.Controllers
{
    public class QueueController : Controller, IQueueProvider
    {
        private readonly IQueueProvider _queueProvider;

        public QueueController(IQueueProvider queueProvider)
        {
            _queueProvider = queueProvider;
        }

        [HttpGet("GetQueues")]
        public IEnumerable<QueueDto> GetQueues()
        {
            return _queueProvider.GetQueues();
        }

        [HttpPost("CreateQueue")]
        public bool CreateQueue(QueueDto queue)
        {
            return _queueProvider.CreateQueue(queue);
        }

        [HttpPut("UpdateQueue")]
        public bool UpdateQueue(QueueDto queue)
        {
            return _queueProvider.UpdateQueue(queue);
        }

        [HttpDelete("DeleteQueue")]
        public bool DeleteQueue(string name)
        {
            return _queueProvider.DeleteQueue(name);
        }
    }
}
