using Domain.Dtos;
using System;
using System.Collections.Generic;

namespace Application.Dojo
{
    public interface IDojoDb
    {
        IEnumerable<RemoteTaskDto> FetchTasks();
        void CreateTask(RemoteTaskDto task);
        void UpdateTask(RemoteTaskDto task);
        void DeleteTask(Guid id);

        IEnumerable<NinjaDto> FetchNinjas();
        void CreateNinja(NinjaDto ninja);
        void DeleteNinja(string address);

        IEnumerable<QueueDto> FetchQueues();
        void CreateQueue(QueueDto queue);
        void UpdateQueue(QueueDto queue);
        void DeleteQueue(string name);
    }
}
