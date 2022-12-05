using Application;
using Application.Dojo;
using Domain.Dtos;
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
        public Guid Execute(string queue, string name, StartTaskDto task)
        {
            return _shogun.Execute(queue, name, task);
        }

        [HttpPost("Cancel")]
        public void Cancel(Guid id)
        {
            _shogun.Cancel(id);
        }
    }
}
