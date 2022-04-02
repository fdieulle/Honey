using Dojo.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Yumi.Domain.Dto;

namespace Dojo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DojoController : Controller
    {
        private readonly Services.Dojo _dojo;

        public DojoController(Services.Dojo dojo)
        {
            _dojo = dojo;
        }

        [HttpGet("Ninjas")]
        public IEnumerable<NinjaDto> GetNinjas()
        {
            return _dojo.Ninjas.Select(p => p.Dto);
        }

        [HttpPost("EnrollNinja")]
        public void EnrollNinja(string address)
        {
            _dojo.EnrollNinja(address);
        }

        [HttpDelete("RevokeNinja")]
        public void RevokeNinja(string address)
        {
            _dojo.RevokeNinja(address);
        }
    }
}
