using Application;
using Application.Beehive;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Beehive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ColonyController : Controller, IColony
    {
        private readonly Colony _colony;

        public ColonyController(Colony colony) 
            => _colony = colony;

        [HttpPost("Execute")]
        public Guid Execute(WorkflowParameters parameters) 
            => _colony.Execute(parameters);

        [HttpPost("ExecuteTask")]
        public Guid ExecuteTask(string name, string queue, TaskParameters task) 
            => _colony.ExecuteTask(name, queue, task);

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
