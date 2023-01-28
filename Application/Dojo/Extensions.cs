using Application.Dojo.Workflows;
using Domain;
using Domain.Dtos;
using System;

namespace Application.Dojo
{
    public static class Extensions
    {
        public static Guid StartTask(this IBee bee, RemoteTaskDto task)
        {
            if (task == null) return Guid.Empty;
            var parameters = task.Parameters;
            if (parameters == null) return Guid.Empty;

            return bee.StartTask(parameters.Command, parameters.Arguments, parameters.NbCores);
        }
    }
}
