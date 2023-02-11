using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Domain.Dtos;
using Application;
using log4net;

namespace Bee.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeeController : ControllerBase, IBee
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Application.Bee.Bee _bee;

        public BeeController(Application.Bee.Bee bee)
        {
            _bee = bee;
        }

        [HttpGet("GetTasks")]
        public IEnumerable<TaskDto> GetTasks()
        {
            return _bee.GetTasks();
        }

        [HttpGet("FetchMessages")]
        public IEnumerable<TaskMessageDto> FetchMessages(Guid id)
        {
            return _bee.FetchMessages(id);
        }

        [HttpPost("StartTask")]
        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            Logger.InfoFormat("Start task: command={0}, arguments={1}, nbCores={2}", command, arguments, nbCores);

            return _bee.StartTask(command, arguments, nbCores);
        }

        [HttpPost("CancelTask")]
        public void CancelTask(Guid id)
        {
            Logger.InfoFormat("Cancel task: id={0}", id);

            _bee.CancelTask(id);
        }

        [HttpDelete("DeleteTask")]
        public void DeleteTask(Guid id)
        {
            Logger.InfoFormat("Delete task: id={0}", id);

            _bee.DeleteTask(id);
        }

        [HttpGet("GetResources")]
        public BeeResourcesDto GetResources()
        {
            return _bee.GetResources();
        }
    }
}
