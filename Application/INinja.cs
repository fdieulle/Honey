using System;
using System.Collections.Generic;
using Domain.Dtos;

namespace Application
{
    public interface INinja
    {
        IEnumerable<TaskDto> GetTasks();

        IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length);

        Guid StartTask(string command, string arguments, int nbCores = 1);

        void CancelTask(Guid id);

        void DeleteTask(Guid id);

        NinjaResourcesDto GetResources();

        void UpdateTask(Guid taskId, double progressPercent, DateTime expectedEndTime, string message = null);
    }

    public interface INinjaContainer
    {
        INinja Resolve(string address);
    }
}
