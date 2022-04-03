using Application.Dojo;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dojo.Controllers
{
    public class QueueController : Controller
    {
        private readonly Shogun _shogun;

        public QueueController(Shogun shogun)
        {
            _shogun = shogun;
        }

        [HttpGet("GetQueues")]
        public IEnumerable<QueueDto> GetQueues()
        {
            return _shogun.Queues;
        }

        [HttpPost("CreateQueue")]
        public void CreateQueue(QueueDto queue)
        {
            _shogun.AddQueue(queue.Name, queue.MaxParallelTasks, queue.Ninjas);
        }

        [HttpPut("UpdateQueue")]
        public void UpdateQueue(QueueDto queue)
        {
            
        }

        [HttpDelete("DeleteQueue")]
        public void DeleteQueue(string name)
        {
            _shogun.RemoveQueue(name);
        }
    }
}
