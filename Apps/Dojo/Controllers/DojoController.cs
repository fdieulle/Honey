using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Domain.Dtos;
using Application;

namespace Dojo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DojoController : Controller, IDojo
    {
        private readonly Application.Dojo.Dojo _dojo;

        public DojoController(Application.Dojo.Dojo dojo)
        {
            _dojo = dojo;
        }

        [HttpGet("Bees")]
        public IEnumerable<BeeDto> GetBees()
        {
            return _dojo.GetBees();
        }

        [HttpPost("EnrollBee")]
        public void EnrollBee(string address)
        {
            _dojo.EnrollBee(address);
        }

        [HttpDelete("RevokeBee")]
        public void RevokeBee(string address)
        {
            _dojo.RevokeBee(address);
        }
    }
}
