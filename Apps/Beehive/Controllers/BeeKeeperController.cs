using Application.Colony;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Honey.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeeKeeperController : Controller
    {
        private readonly BeeKeeper _beeKeeper;

        public BeeKeeperController(BeeKeeper beeKeeper) => _beeKeeper = beeKeeper;

        [HttpGet("Bees")]
        public List<BeeDto> GetBees()
            => _beeKeeper.GetBees();

        [HttpPost("EnrollBee")]
        public bool EnrollBee(string address)
            => _beeKeeper.EnrollBee(address);

        [HttpDelete("RevokeBee")]
        public bool RevokeBee(string address)
            => _beeKeeper.RevokeBee(address);
    }
}
