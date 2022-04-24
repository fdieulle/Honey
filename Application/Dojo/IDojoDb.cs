using Domain.Dtos;
using System;
using System.Collections.Generic;

namespace Application.Dojo
{
    public interface IDojoDb
    {
        IEnumerable<QueuedTaskDto> FetchTasks();
        void CreateTask(QueuedTaskDto task);
        void UpdateTask(QueuedTaskDto task);
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
