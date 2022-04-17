using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dojo
{
    public static class Extensions
    {
        public static Guid StartTask(this Ninja ninja, QueueTaskDto task)
        {
            var startTask = task.StartTask;
            if (startTask == null) return Guid.Empty;

            return ninja.StartTask(startTask.Command, startTask.Arguments, startTask.NbCores);
        }
    }
}
