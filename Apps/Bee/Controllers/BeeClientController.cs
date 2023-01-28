using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bee.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeeClientController
    {
        private readonly Application.Bee.Bee _bee;
        private readonly ILogger<BeeClientController> _logger;

        public BeeClientController(Application.Bee.Bee bee, ILogger<BeeClientController> logger)
        {
            _bee = bee;
            _logger = logger;
        }

        [HttpPost("UpdateTask")]
        public void UpdateTask(TaskStateDto dto)
        {
            _logger.LogInformation("Update task: id={0}, progress={1}, message={2}", dto.TaskId, dto.ProgressPercent, dto.Message);

            _bee.UpdateTask(dto);
        }
    }
}
