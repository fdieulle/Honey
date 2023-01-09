using Domain.Dtos;
using Domain.Dtos.Pipelines;
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

        IEnumerable<JobDto> FetchJobs();
        JobDto FetchJob(Guid id);
        void CreateJob(JobDto job);
        void UpdateJob(JobDto job);
        void DeleteJob(Guid id);

        IEnumerable<PipelineDto> FetchPipelines();
        PipelineDto FetchPipeline(Guid id);
        void CreatePipeline(PipelineDto pipeline);
        void UpdatePipeline(PipelineDto pipeline);
        void DeletePipeline(Guid id);
    }
}
