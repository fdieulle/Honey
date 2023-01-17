using Application.Dojo.Workflows;
using Domain;
using Domain.Dtos;
using System;

namespace Application.Dojo
{
    public static class Extensions
    {
        public static Guid StartTask(this INinja ninja, RemoteTaskDto task)
        {
            if (task == null) return Guid.Empty;
            var startTask = task.StartTask;
            if (startTask == null) return Guid.Empty;

            return ninja.StartTask(startTask.Command, startTask.Arguments, startTask.NbCores);
        }
    }
}
