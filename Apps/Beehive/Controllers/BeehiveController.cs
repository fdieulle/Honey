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
        public List<BeeDto> GetBees() 
            => _beehive.GetBees();

        [HttpPost("EnrollBee")]
        public bool EnrollBee(string address) 
            => _beehive.EnrollBee(address);

        [HttpDelete("RevokeBee")]
        public bool RevokeBee(string address)
            => _beehive.RevokeBee(address);
    }
}
