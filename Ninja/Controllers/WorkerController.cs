using Microsoft.AspNetCore.Mvc;
using Ninja.Dto;
using Ninja.Services;
using System;
using System.Collections.Generic;

namespace Ninja.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkerController : ControllerBase
    {
        private readonly Worker _worker;

        public WorkerController(Worker worker)
        {
            _worker = worker;
        }

        [HttpGet("GetJobs")]
        public IEnumerable<Job> GetJobs()
        {
            return _worker.GetJobs();
        }

        [HttpGet("FetchMessages")]
        public IEnumerable<JobMessage> FetchMessages(Guid id, int start, int length)
        {
            return _worker.FetchMessages(id, start, length);
        }

        [HttpPost("StartJob")]
        public Guid StartJob(string command, string arguments, int nbCores = 1)
        {
            return _worker.StartJob(command, arguments, nbCores);
        }

        [HttpPost("CancelJob")]
        public void CancelJob(Guid id)
        {
            _worker.CancelJob(id);
        }

        [HttpDelete("DeleteJob")]
        public void DeleteJob(Guid id)
        {
            _worker.DeleteJob(id);
        }

        [HttpGet("GetResources")]
        public WorkerResources GetResources()
        {
            return _worker.GetResources();
        }
    }
}
