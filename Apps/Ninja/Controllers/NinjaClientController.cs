using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ninja.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NinjaClientController
    {
        private readonly Application.Ninja.Ninja _ninja;
        private readonly ILogger<NinjaClientController> _logger;

        public NinjaClientController(Application.Ninja.Ninja ninja, ILogger<NinjaClientController> logger)
        {
            _ninja = ninja;
            _logger = logger;
        }

        [HttpPost("UpdateTask")]
        public void UpdateTask(TaskStateDto dto)
        {
            _logger.LogInformation("Update task: id={0}, progress={1}, message={2}", dto.TaskId, dto.ProgressPercent, dto.Message);

            _ninja.UpdateTask(dto);
        }
    }
}
