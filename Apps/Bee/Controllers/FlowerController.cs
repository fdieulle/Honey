using Application;
using Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bee.Controllers
{
    // TODO: Rename to flower ?
    [ApiController]
    [Route("[controller]")]
    public class FlowerController : IFlower
    {
        private readonly Application.Bee.Bee _bee;
        private readonly ILogger<FlowerController> _logger;

        public FlowerController(Application.Bee.Bee bee, ILogger<FlowerController> logger)
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
