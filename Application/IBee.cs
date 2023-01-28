using System;
using System.Collections.Generic;
using Domain.Dtos;

namespace Application
{
    public interface IBee
    {
        IEnumerable<TaskDto> GetTasks();

        IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length);

        Guid StartTask(string command, string arguments, int nbCores = 1);

        void CancelTask(Guid id);

        void DeleteTask(Guid id);

        BeeResourcesDto GetResources();
    }

    public interface IBeeClient
    {
        void UpdateTask(TaskStateDto state);
    }

    public interface IBeeFactory
    {
        IBee Create(string address);
    }
}
