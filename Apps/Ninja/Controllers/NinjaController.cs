using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Domain.Dtos;
using Application;

namespace Ninja.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NinjaController : ControllerBase, INinja
    {
        private readonly Application.Ninja.Ninja _ninja;

        public NinjaController(Application.Ninja.Ninja ninja)
        {
            _ninja = ninja;
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
            return _ninja.StartTask(command, arguments, nbCores);
        }

        [HttpPost("CancelTask")]
        public void CancelTask(Guid id)
        {
            _ninja.CancelTask(id);
        }

        [HttpDelete("DeleteTask")]
        public void DeleteTask(Guid id)
        {
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
            _ninja.UpdateTask(dto);
        }
    }
}
