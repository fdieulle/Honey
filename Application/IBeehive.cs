using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Dtos;
using Domain.Dtos.Workflows;

namespace Application
{
    public interface IBeeKeeper
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

    public interface IBeehive
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

        public async static ValueTask<Guid> ExecuteAsync(this IBeehive beehive, WorkflowParameters parameters) 
            => await ValueTask.FromResult(beehive.Execute(parameters));

        public async static ValueTask<Guid> ExecuteTaskAsync(this IBeehive beehive, string queue, string name, TaskParameters task)
            => await ValueTask.FromResult(beehive.ExecuteTask(queue, name, task));

        public async static ValueTask<bool> CancelAsync(this IBeehive beehive, Guid id)
            => await ValueTask.FromResult(beehive.Cancel(id));

        public async static ValueTask<bool> RecoverAsync(this IBeehive beehive, Guid id)
            => await ValueTask.FromResult(beehive.Recover(id));

        public async static ValueTask<bool> DeleteAsync(this IBeehive beehive, Guid id)
            => await ValueTask.FromResult(beehive.Delete(id));

        public async static Task<List<RemoteTaskDto>> GetTasksAsync(this IBeehive beehive)
            => await ValueTask.FromResult(beehive.GetTasks());

        public async static Task<List<JobDto>> GetJobsAsync(this IBeehive beehive)
            => await ValueTask.FromResult(beehive.GetJobs());

        public async static Task<List<WorkflowDto>> GetWorkflowsAsync(this IBeehive beehive)
            => await ValueTask.FromResult(beehive.GetWorkflows());

        #endregion

        #region Async for IBeehive

        public async static Task<List<BeeDto>> GetBeesAsync(this IBeeKeeper beeKeeper)
            => await Task.FromResult(beeKeeper.GetBees());

        public async static ValueTask<bool> EnrollBeeAsync(this IBeeKeeper beeKeeper, string address)
            => await Task.FromResult(beeKeeper.EnrollBee(address));

        public async static ValueTask<bool> RevokeBeeAsync(this IBeeKeeper beeKeeper, string address)
            => await Task.FromResult(beeKeeper.RevokeBee(address));

        #endregion
    }

    public interface ITaskTracker
    {
        IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate);
    }

    // TODO: ReRun a task in error by keeping the same Id
    // Todo: Handle the task deletion

    
}
