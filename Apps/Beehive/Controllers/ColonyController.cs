using Application;
using Application.Beehive;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Beehive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ColonyController : Controller, IColonyProvider
    {
        private readonly ColonyProvider _colonyProvider;

        public ColonyController(ColonyProvider colonyProvider)
        {
            _colonyProvider = colonyProvider;
        }

        [HttpGet("GetColonies")]
        public IEnumerable<ColonyDto> GetColonies()
        {
            return _colonyProvider.GetColonies();
        }

        [HttpPost("CreateColony")]
        public bool CreateColony(ColonyDto colony)
        {
            return _colonyProvider.CreateColony(colony);
        }

        [HttpPut("UpdateColony")]
        public bool UpdateColony(ColonyDto colony)
        {
            return _colonyProvider.UpdateColony(colony);
        }

        [HttpDelete("DeleteColony")]
        public bool DeleteColony(string name)
        {
            return _colonyProvider.DeleteColony(name);
        }
    }
}
