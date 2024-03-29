﻿using Domain;
using Domain.Dtos.Workflows;
using Domain.Dtos;
using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Application.Honey;
using Application.Colony.Workflows;
using System.Threading.Tasks;
using System.IO;

namespace Application.Honey
{
    public static class Extensions
    {
        #region Workflow

        public static WorkflowViewModel ToViewModel(this WorkflowDto dto) => new WorkflowViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Beehive = dto.Beehive,
            Owner = dto.Owner,
        };

        public static void Update(this WorkflowViewModel workflow, JobViewModel rootJob)
        {
            workflow.Status = rootJob.Status;
            workflow.Progress = rootJob.Progress;
            workflow.Duration = rootJob.Duration;
            workflow.StartTime = rootJob.StartTime;
            workflow.CanCancel= rootJob.CanCancel;
            workflow.CanRecover = rootJob.CanRecover;
            workflow.CanDelete = rootJob.CanDelete;
        }

        public static void Update(this WorkflowViewModel vm, WorkflowDto dto)
        {
            vm.Name = dto.Name;
            vm.Beehive = dto.Beehive;
            vm.Owner = dto.Owner;
        }

        #endregion

        #region Job

        public static JobViewModel ToViewModel(this JobDto dto)
        {
            var vm = dto is SingleTaskJobDto ? new HostedJobViewModel() : new JobViewModel();

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
            vm.ColonyTaskId = dto.TaskId;

            vm.Type = dto.Parameters.Command;
            if (!string.IsNullOrEmpty(vm.Type))
                vm.Type = Path.GetFileName(vm.Type);

            if (!tasks.TryGetValue(dto.TaskId, out var task))
                return;

            vm.Host = task.BeeAddress;
            var beeState = task.BeeState;
            if (beeState != null)
            {
                vm.BeeTaskId = beeState.Id;
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

            if (vm.Children.Count > 0)
            {
                vm.CanCancel = vm.Children.Any(p => p.Status.CanCancel());
                vm.CanRecover = vm.Children.Any(p => p.Status.CanRecover());
                vm.CanDelete = vm.Children.Any(p => p.Status.CanDelete());
            }
            else
            {
                vm.CanCancel = vm.Status.CanCancel();
                vm.CanRecover = vm.Status.CanRecover();
                vm.CanDelete = vm.Status.CanDelete();
            }

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

        #region Async
        
        public static async Task<IDisposable> SubscribeAsync<TValue>(this IRepository<TValue> respository, IList<TValue> view)
            => await Task.FromResult(respository.Subscribe(view));

        public static async Task<List<WorkflowViewModel>> GetWorkflowsAsync(this WorkflowRepository repository)
            => await Task.FromResult(repository.GetWorkflows());

        public static async Task<WorkflowViewModel> GetWorkflowAsync(this WorkflowRepository repository, Guid id)
            => await Task.FromResult(repository.GetWorkflow(id));

        #endregion
    }
}
