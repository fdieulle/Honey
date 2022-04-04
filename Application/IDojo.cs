using System;
using System.Collections.Generic;
using Domain.Dtos;

namespace Application
{
    public interface IDojo
    {
        IEnumerable<NinjaDto> GetNinjas();
        
        void EnrollNinja(string address);
        
        void RevokeNinja(string address);
    }

    public interface IQueueProvider
    {
        IEnumerable<QueueDto> GetQueues();

        bool CreateQueue(QueueDto queue);

        bool UpdateQueue(QueueDto queue);

        bool DeleteQueue(string name);
    }

    public interface IShogun
    {
        Guid Execute(string queue, StartTaskDto task);

        void Cancel(Guid id);
    }
}
