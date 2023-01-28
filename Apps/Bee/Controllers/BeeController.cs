using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Domain.Dtos;
using Application;
using Microsoft.Extensions.Logging;

namespace Bee.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeeController : ControllerBase, IBee
    {
        private readonly Application.Bee.Bee _bee;
        private readonly ILogger<BeeController> _logger;

        public BeeController(Application.Bee.Bee bee, ILogger<BeeController> logger)
        {
            _bee = bee;
            _logger = logger;
        }

        [HttpGet("GetTasks")]
        public IEnumerable<TaskDto> GetTasks()
        {
            return _bee.GetTasks();
        }

        [HttpGet("FetchMessages")]
        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length)
        {
            return _bee.FetchMessages(id, start, length);
        }

        [HttpPost("StartTask")]
        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            _logger.LogInformation("Start task: command={0}, arguments={1}, nbCores={2}", command, arguments, nbCores);

            return _bee.StartTask(command, arguments, nbCores);
        }

        [HttpPost("CancelTask")]
        public void CancelTask(Guid id)
        {
            _logger.LogInformation("Cancel task: id={0}", id);

            _bee.CancelTask(id);
        }

        [HttpDelete("DeleteTask")]
        public void DeleteTask(Guid id)
        {
            _logger.LogInformation("Delete task: id={0}", id);

            _bee.DeleteTask(id);
        }

        [HttpGet("GetResources")]
        public BeeResourcesDto GetResources()
        {
            return _bee.GetResources();
        }
    }
}
