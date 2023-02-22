using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Dtos;

namespace Application
{
    public interface IBee
    {
        IEnumerable<TaskDto> GetTasks();

        Task<List<string>> FetchLogsAsync(Guid id, int start = 0, int length = -1);

        Guid StartTask(string command, string arguments, int nbCores = 1);

        void CancelTask(Guid id);

        void DeleteTask(Guid id);

        BeeResourcesDto GetResources();
    }

    public interface IFlower
    {
        void UpdateTask(TaskStateDto state);
    }

    public interface IBeeFactory
    {
        IBee Create(string address);
    }
}
