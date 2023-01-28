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
        {
            _colony = colony;
        }

        [HttpPost("Execute")]
        public Guid Execute(WorkflowParameters parameters)
        {
            return _colony.Execute(parameters);
        }

        [HttpPost("ExecuteTask")]
        public Guid ExecuteTask(string name, string queue, TaskParameters task)
        {
            return _colony.ExecuteTask(name, queue, task);
        }

        [HttpPost("Cancel")]
        public void Cancel(Guid id)
        {
            _colony.Cancel(id);
        }

        [HttpPost("Recover")]
        public void Recover(Guid id)
        {
            _colony.Recover(id);
        }

        [HttpPost("Delete")]
        public void Delete(Guid id)
        {
            _colony.Delete(id);
        }

        [HttpGet("GetTasks")]
        public List<RemoteTaskDto> GetTasks() 
        {
            return _colony.GetTasks();
        }

        [HttpGet("GetJobs")]
        public List<JobDto> GetJobs() {
            return _colony.GetJobs();
        }

        [HttpGet("GetWorkflows")]
        public List<WorkflowDto> GetWorkflows() {
            return _colony.GetWorkflows();
        }
    }
}
