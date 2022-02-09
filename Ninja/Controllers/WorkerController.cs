using Microsoft.AspNetCore.Mvc;
using Ninja.Dto;
using Ninja.Services;
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
        public IEnumerable<JobMessage> FetchMessages(string id, int start, int length)
        {
            return _worker.FetchMessages(id, start, length);
        }

        [HttpPost("StartJob")]
        public string StartJob(string name, string command, string arguments, int nbCores = 1)
        {
            return _worker.StartJob(name, command, arguments, nbCores);
        }

        [HttpPost("CancelJob")]
        public void CancelJob(string id)
        {
            _worker.CancelJob(id);
        }

        [HttpDelete("DeleteJob")]
        public void DeleteJob(string id)
        {
            _worker.DeleteJob(id);
        }
    }
}
