using Microsoft.AspNetCore.Mvc;
using System;

namespace Dojo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShogunController : Controller
    {
        [HttpPost("Execute")]
        public Guid Execute(string queue, string command, string arguments, int nbCores = 1)
        {
            return Guid.Empty;
        }

        [HttpPost("Cancel")]
        public void Cancel(Guid id)
        {
            
        }
    }
}
