using Application;
using Application.Dojo;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Dojo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShogunController : Controller, IShogun
    {
        private readonly Shogun _shogun;

        public ShogunController(Shogun shogun)
        {
            _shogun = shogun;
        }

        [HttpPost("Execute")]
        public Guid Execute(WorkflowParameters parameters)
        {
            return _shogun.Execute(parameters);
        }

        [HttpPost("ExecuteTask")]
        public Guid ExecuteTask(string name, string queue, TaskParameters task)
        {
            return _shogun.ExecuteTask(name, queue, task);
        }

        [HttpPost("Cancel")]
        public void Cancel(Guid id)
        {
            _shogun.Cancel(id);
        }

        [HttpPost("Delete")]
        public void Delete(Guid id)
        {
            _shogun.Delete(id);
        }
    }
}
