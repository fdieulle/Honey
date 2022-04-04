using Application;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Dojo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShogunController : Controller, IShogun
    {
        private readonly IShogun _shogun;

        public ShogunController(IShogun shogun)
        {
            _shogun = shogun;
        }

        [HttpPost("Execute")]
        public Guid Execute(string queue, StartTaskDto task)
        {
            return _shogun.Execute(queue, task);
        }

        [HttpPost("Cancel")]
        public void Cancel(Guid id)
        {
            _shogun.Cancel(id);
        }
    }
}
