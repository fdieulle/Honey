using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;

namespace Application.Beehive
{
    public interface IBeehiveDb
    {
        IEnumerable<RemoteTaskDto> FetchTasks();
        void CreateTask(RemoteTaskDto task);
        void UpdateTask(RemoteTaskDto task);
        void DeleteTask(Guid id);

        IEnumerable<BeeDto> FetchBees();
        void CreateBee(BeeDto bee);
        void DeleteBee(string address);

        IEnumerable<QueueDto> FetchQueues();
        void CreateQueue(QueueDto queue);
        void UpdateQueue(QueueDto queue);
        void DeleteQueue(string name);

        IEnumerable<JobDto> FetchJobs();
        JobDto FetchJob(Guid id);
        void CreateJob(JobDto job);
        void UpdateJob(JobDto job);
        void DeleteJob(Guid id);

        IEnumerable<WorkflowDto> FetchWorkflows();
        WorkflowDto FetchWorkflow(Guid id);
        void CreateWorkflow(WorkflowDto workflow);
        void UpdateWorkflow(WorkflowDto workflow);
        void DeleteWorkflow(Guid id);
    }
}
