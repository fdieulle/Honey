using Application;
using Application.Colony;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Honey.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeehiveController : Controller, IBeehiveProvider
    {
        private readonly BeehiveProvider _beehiveProvider;

        public BeehiveController(BeehiveProvider beehiveProvider)
        {
            _beehiveProvider = beehiveProvider;
        }

        [HttpGet("GetBeehives")]
        public IEnumerable<BeehiveDto> GetBeehives()
        {
            return _beehiveProvider.GetBeehives();
        }

        [HttpPost("CreateBeehive")]
        public bool CreateBeehive(BeehiveDto beehive)
        {
            return _beehiveProvider.CreateBeehive(beehive);
        }

        [HttpPut("UpdateBeehive")]
        public bool UpdateBeehive(BeehiveDto beehive)
        {
            return _beehiveProvider.UpdateBeehive(beehive);
        }

        [HttpDelete("DeleteBeehive")]
        public bool DeleteBeehive(string name)
        {
            return _beehiveProvider.DeleteBeehive(name);
        }
    }
}
