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

    public interface IBeehiveProvider
    {
        IEnumerable<BeehiveDto> GetBeehives();

        bool CreateBeehive(BeehiveDto beehive);

        bool UpdateBeehive(BeehiveDto beehive);

        bool DeleteBeehive(string name);
    }

    public interface IColony
    {
        Guid Execute(WorkflowParameters parameters);

        Guid ExecuteTask(string name, string beehive, string owner, TaskParameters task);
        
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

        public async static ValueTask<Guid> ExecuteTaskAsync(this IColony colony, string beehive, string name, string owner, TaskParameters task)
            => await ValueTask.FromResult(colony.ExecuteTask(name, beehive, owner, task));

        public async static ValueTask<bool> CancelAsync(this IColony colony, Guid id)
            => await ValueTask.FromResult(colony.Cancel(id));

        public async static ValueTask<bool> RecoverAsync(this IColony colony, Guid id)
            => await ValueTask.FromResult(colony.Recover(id));

        public async static ValueTask<bool> DeleteAsync(this IColony colony, Guid id) 
            => await ValueTask.FromResult(colony.Delete(id));

        public async static Task<List<RemoteTaskDto>> GetTasksAsync(this IColony colony)
            => await Task.FromResult(colony.GetTasks());

        public async static Task<List<JobDto>> GetJobsAsync(this IColony colony)
            => await Task.FromResult(colony.GetJobs());

        public async static Task<List<WorkflowDto>> GetWorkflowsAsync(this IColony colony)
            => await Task.FromResult(colony.GetWorkflows());

        #endregion

        #region Async for IBeeKeeper

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
        IDisposable Subscribe(Action<IEnumerable<RemoteTaskDto>> onUpdate);

        IWorkflowTaskTracker CreateScope(IDispatcher dispatcher);
    }

    public interface IWorkflowTaskTracker : IDisposable
    {
        IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate);
    }

    // TODO: ReRun a task in error by keeping the same Id
    // Todo: Handle the task deletion

    
}
