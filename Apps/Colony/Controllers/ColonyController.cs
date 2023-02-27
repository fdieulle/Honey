using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Domain.Dtos;
using Application;
using Domain.Dtos.Workflows;
using System;

namespace Honey.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ColonyController : Controller, IColony
    {
        private readonly Application.Colony.Colony _colony;

        public ColonyController(Application.Colony.Colony colony)
            => _colony = colony;

        [HttpPost("Execute")]
        public Guid Execute(WorkflowParameters parameters)
            => _colony.Execute(parameters);

        [HttpPost("ExecuteTask")]
        public Guid ExecuteTask(string name, string beehive, string owner, TaskParameters task)
            => _colony.ExecuteTask(name, beehive, owner, task);

        [HttpPost("Cancel")]
        public bool Cancel(Guid id)
            => _colony.Cancel(id);

        [HttpPost("Recover")]
        public bool Recover(Guid id)
            => _colony.Recover(id);

        [HttpPost("Delete")]
        public bool Delete(Guid id)
            => _colony.Delete(id);

        [HttpGet("GetTasks")]
        public List<RemoteTaskDto> GetTasks()
            => _colony.GetTasks();

        [HttpGet("GetJobs")]
        public List<JobDto> GetJobs()
            => _colony.GetJobs();

        [HttpGet("GetWorkflows")]
        public List<WorkflowDto> GetWorkflows()
            => _colony.GetWorkflows();
    }
}
