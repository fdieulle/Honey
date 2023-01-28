using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Domain.Dtos;
using Application;

namespace Beehive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeehiveController : Controller, IBeehive
    {
        private readonly Application.Beehive.Beehive _beehive;

        public BeehiveController(Application.Beehive.Beehive beehive)
        {
            _beehive = beehive;
        }

        [HttpGet("Bees")]
        public IEnumerable<BeeDto> GetBees()
        {
            return _beehive.GetBees();
        }

        [HttpPost("EnrollBee")]
        public void EnrollBee(string address)
        {
            _beehive.EnrollBee(address);
        }

        [HttpDelete("RevokeBee")]
        public void RevokeBee(string address)
        {
            _beehive.RevokeBee(address);
        }
    }
}
