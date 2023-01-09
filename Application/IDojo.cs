using System;
using System.Collections.Generic;
using Domain.Dtos;
using Domain.Dtos.Pipelines;

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
        Guid Execute(PipelineParameters parameters);

        Guid ExecuteTask(string queue, string name, TaskParameters task);

        void Cancel(Guid id);

        void Delete(Guid id);
    }

    public interface ITaskTracker
    {
        IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate);
    }

    // TODO: ReRun a task in error by keeping the same Id
    // Todo: Handle the task deletion

    
}
