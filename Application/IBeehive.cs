using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Beehive;
using Domain.Dtos;
using Domain.Dtos.Workflows;

namespace Application
{
    public interface IBeehive
    {
        List<BeeDto> GetBees();
        
        bool EnrollBee(string address);
        
        bool RevokeBee(string address);
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
        
        bool Cancel(Guid id);

        bool Recover(Guid id);

        bool Delete(Guid id);

        List<RemoteTaskDto> GetTasks();
        List<JobDto> GetJobs();
        List<WorkflowDto> GetWorkflows();
    }

    public static class ColonyExtensions
    {
        #region Async for IColony

        public async static ValueTask<Guid> ExecuteAsync(this IColony colony, WorkflowParameters parameters) 
            => await ValueTask.FromResult(colony.Execute(parameters));

        public async static ValueTask<Guid> ExecuteTaskAsync(this IColony colony, string queue, string name, TaskParameters task)
            => await ValueTask.FromResult(colony.ExecuteTask(queue, name, task));

        public async static ValueTask<bool> CancelAsync(this IColony colony, Guid id)
            => await ValueTask.FromResult(colony.Cancel(id));

        public async static ValueTask<bool> RecoverAsync(this IColony colony, Guid id)
            => await ValueTask.FromResult(colony.Recover(id));

        public async static ValueTask<bool> DeleteAsync(this IColony colony, Guid id)
            => await ValueTask.FromResult(colony.Delete(id));

        public async static Task<List<RemoteTaskDto>> GetTasksAsync(this IColony colony)
            => await ValueTask.FromResult(colony.GetTasks());

        public async static Task<List<JobDto>> GetJobsAsync(this IColony colony)
            => await ValueTask.FromResult(colony.GetJobs());

        public async static Task<List<WorkflowDto>> GetWorkflowsAsync(this IColony colony)
            => await ValueTask.FromResult(colony.GetWorkflows());

        #endregion

        #region Async for IBeehive

        public async static Task<List<BeeDto>> GetBeesAsync(this IBeehive beehive)
            => await Task.FromResult(beehive.GetBees());

        public async static ValueTask<bool> EnrollBeeAsync(this IBeehive beehive, string address)
            => await Task.FromResult(beehive.EnrollBee(address));

        public async static ValueTask<bool> RevokeBeeAsync(this IBeehive beehive, string address)
            => await Task.FromResult(beehive.RevokeBee(address));

        #endregion
    }

    public interface ITaskTracker
    {
        IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate);
    }

    // TODO: ReRun a task in error by keeping the same Id
    // Todo: Handle the task deletion

    
}
