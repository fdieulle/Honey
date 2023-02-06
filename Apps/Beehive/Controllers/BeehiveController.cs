using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Domain.Dtos;
using Application;
using Domain.Dtos.Workflows;
using System;

namespace Beehive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeehiveController : Controller, IBeehive
    {
        private readonly Application.Beehive.Beehive _beehive;

        public BeehiveController(Application.Beehive.Beehive beehive)
            => _beehive = beehive;

        [HttpPost("Execute")]
        public Guid Execute(WorkflowParameters parameters)
            => _beehive.Execute(parameters);

        [HttpPost("ExecuteTask")]
        public Guid ExecuteTask(string name, string colony, TaskParameters task)
            => _beehive.ExecuteTask(name, colony, task);

        [HttpPost("Cancel")]
        public bool Cancel(Guid id)
            => _beehive.Cancel(id);

        [HttpPost("Recover")]
        public bool Recover(Guid id)
            => _beehive.Recover(id);

        [HttpPost("Delete")]
        public bool Delete(Guid id)
            => _beehive.Delete(id);

        [HttpGet("GetTasks")]
        public List<RemoteTaskDto> GetTasks()
            => _beehive.GetTasks();

        [HttpGet("GetJobs")]
        public List<JobDto> GetJobs()
            => _beehive.GetJobs();

        [HttpGet("GetWorkflows")]
        public List<WorkflowDto> GetWorkflows()
            => _beehive.GetWorkflows();
    }
}
