using Application;
using Domain.Dtos;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bee.Controllers
{
    // TODO: Rename to flower ?
    [ApiController]
    [Route("[controller]")]
    public class FlowerController : IFlower
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Application.Bee.Bee _bee;

        public FlowerController(Application.Bee.Bee bee)
        {
            _bee = bee;
        }

        [HttpPost("UpdateTask")]
        public void UpdateTask(TaskStateDto dto)
        {
            Logger.InfoFormat("Update task: id={0}, progress={1}, message={2}", dto.TaskId, dto.ProgressPercent, dto.Message);

            _bee.UpdateTask(dto);
        }
    }
}
