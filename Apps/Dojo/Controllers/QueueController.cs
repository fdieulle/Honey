using Application;
using Application.Dojo;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Dojo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : Controller, IQueueProvider
    {
        private readonly QueueProvider _queueProvider;

        public QueueController(QueueProvider queueProvider)
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
