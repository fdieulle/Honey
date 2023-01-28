using System;
using System.Collections.Generic;
using Domain.Dtos;
using Domain.Dtos.Workflows;

namespace Application
{
    public interface IDojo
    {
        IEnumerable<BeeDto> GetBees();
        
        void EnrollBee(string address);
        
        void RevokeBee(string address);
    }

    public interface IQueueProvider
    {
        IEnumerable<QueueDto> GetQueues();

        bool CreateQueue(QueueDto queue);

        bool UpdateQueue(QueueDto queue);

        bool DeleteQueue(string name);
    }

    public interface IColony
    {
        Guid Execute(WorkflowParameters parameters);

        Guid ExecuteTask(string queue, string name, TaskParameters task);

        void Cancel(Guid id);

        void Recover(Guid id);

        void Delete(Guid id);

        List<RemoteTaskDto> GetTasks();
        List<JobDto> GetJobs();
        List<WorkflowDto> GetWorkflows();
    }

    public interface ITaskTracker
    {
        IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate);
    }

    // TODO: ReRun a task in error by keeping the same Id
    // Todo: Handle the task deletion

    
}
