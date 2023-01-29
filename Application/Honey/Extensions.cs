using Domain;
using Domain.Dtos.Workflows;
using Domain.Dtos;
using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Application.Honey;
using Application.Beehive.Workflows;

namespace Application.Honey
{
    public static class Extensions
    {
        #region Workflow

        public static WorkflowViewModel ToViewModel(this WorkflowDto dto) => new WorkflowViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Queue = dto.QueueName,
        };

        public static void Update(this WorkflowViewModel workflow, JobViewModel rootJob)
        {
            workflow.Status = rootJob.Status;
            workflow.Progress = rootJob.Progress;
            workflow.Duration = rootJob.Duration;
            workflow.StartTime = rootJob.StartTime;
        }

        public static void Update(this WorkflowViewModel vm, WorkflowDto dto)
        {
            vm.Name = dto.Name;
            vm.Queue = dto.QueueName;
        }

        #endregion

        #region Job

        public static JobViewModel ToViewModel(this JobDto dto)
        {
            var vm = dto is SingleTaskJobDto st ? new HostedJobViewModel() : new JobViewModel();

            vm.Id = dto.Id;
            vm.Name = dto.Name;
            vm.Status = dto.Status;

            return vm;
        }

        public static void Update(this JobViewModel vm, JobDto dto)
        {
            vm.Name = dto.Name;
            vm.Status = dto.Status;
        }

        public static void Update(this JobViewModel vm, JobDto dto, Dictionary<Guid, JobViewModel> jobs, Dictionary<Guid, RemoteTaskDto> tasks)
        {
            if (dto is SingleTaskJobDto sj) // This is a leaf
            {
                ((HostedJobViewModel)vm).Update(sj, tasks);
                return;
            }

            if (dto is ManyJobsDto mj)
            {
                vm.Type = mj.Behavior.ToString();
                vm.Children.Clear();
                vm.Children.AddRange(mj.JobIds.Select(jobs.Get));
            }
        }

        private static void Update(this HostedJobViewModel vm, SingleTaskJobDto dto, Dictionary<Guid, RemoteTaskDto> tasks)
        {
            vm.Type = dto.Parameters.Command;
            if (!tasks.TryGetValue(dto.TaskId, out var task))
                return;

            vm.Host = task.BeeAddress;
            var beeState = task.BeeState;
            if (beeState != null)
            {
                vm.HostId = beeState.Id;
                vm.StartTime = beeState.StartTime;
                vm.Progress = beeState.ProgressPercent * 100;
                vm.Duration = (beeState.IsFinalStatus() ? beeState.EndTime : DateTime.Now) - beeState.StartTime;
            }

            var parameters = task.Parameters;
            if (parameters != null)
            {
                vm.Command = parameters.Command;
                vm.Arguments = parameters.Arguments;
                vm.NbCores = parameters.NbCores;
            }
        }

        private static JobViewModel Get(this Dictionary<Guid, JobViewModel> jobs, Guid id)
            => jobs.TryGetValue(id, out var job) ? job : JobViewModel.Empty;

        private static readonly string parallel = JobsBehavior.Parallel.ToString();
        private static readonly string sequential = JobsBehavior.Sequential.ToString();

        public static void UpdateTree(this JobViewModel vm)
        {
            if (vm == null || vm.Children == null) return;

            if (vm.Type == parallel)
                vm.UpdateParallel();
            else if (vm.Type == sequential)
                vm.UpdateSequential();

            if (vm.Status == JobStatus.Completed)
                vm.Progress = 100;
        }

        private static void UpdateParallel(this JobViewModel vm)
        {
            vm.StartTime = null;
            vm.Progress = 100.0;
            vm.Duration = TimeSpan.Zero;
            foreach (var job in vm.Children)
            {
                job.UpdateTree();

                vm.UpdateStartTime(job.StartTime);
                vm.Progress = Math.Min(vm.Progress, job.Progress);
                vm.Duration = Max(vm.Duration, job.Duration);
            }
        }

        private static void UpdateSequential(this JobViewModel vm)
        {
            var count = (double)vm.Children.Count;

            vm.StartTime = null;
            vm.Progress = 0;
            vm.Duration = TimeSpan.Zero;
            foreach (var job in vm.Children)
            {
                job.UpdateTree();

                vm.UpdateStartTime(job.StartTime);
                vm.Progress += Math.Ceiling(job.Progress / count);
                vm.Duration += job.Duration;
            }
            vm.Progress = Math.Min(100, vm.Progress);
        }

        private static void UpdateStartTime(this JobViewModel vm, DateTime? jobStartTime)
        {
            if (!jobStartTime.HasValue)
                return;

            vm.StartTime = vm.StartTime.HasValue
                ? Min(vm.StartTime.Value, jobStartTime.Value)
                : jobStartTime.Value;
        }

        private static DateTime Min(DateTime x, DateTime y) => x < y ? x : y;
        private static TimeSpan Max(TimeSpan x, TimeSpan y) => x > y ? x : y;

        #endregion
    }
}
