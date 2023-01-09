using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Domain.Dtos;
using Application;
using Microsoft.Extensions.Logging;

namespace Ninja.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NinjaController : ControllerBase, INinja
    {
        private readonly Application.Ninja.Ninja _ninja;
        private readonly ILogger<NinjaController> _logger;

        public NinjaController(Application.Ninja.Ninja ninja, ILogger<NinjaController> logger)
        {
            _ninja = ninja;
            _logger = logger;
        }

        [HttpGet("GetTasks")]
        public IEnumerable<TaskDto> GetTasks()
        {
            return _ninja.GetTasks();
        }

        [HttpGet("FetchMessages")]
        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length)
        {
            return _ninja.FetchMessages(id, start, length);
        }

        [HttpPost("StartTask")]
        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            _logger.LogInformation("Start task: command={0}, arguments={1}, nbCores={2}", command, arguments, nbCores);

            return _ninja.StartTask(command, arguments, nbCores);
        }

        [HttpPost("CancelTask")]
        public void CancelTask(Guid id)
        {
            _logger.LogInformation("Cancel task: id={0}", id);

            _ninja.CancelTask(id);
        }

        [HttpDelete("DeleteTask")]
        public void DeleteTask(Guid id)
        {
            _logger.LogInformation("Delete task: id={0}", id);

            _ninja.DeleteTask(id);
        }

        [HttpGet("GetResources")]
        public NinjaResourcesDto GetResources()
        {
            return _ninja.GetResources();
        }

        [HttpPost("UpdateTask")]
        public void UpdateTask(TaskStateDto dto)
        {
            _logger.LogInformation("Update task: id={0}, progress={1}, message={2}", dto.TaskId, dto.ProgressPercent, dto.Message);

            _ninja.UpdateTask(dto);
        }
    }
}
