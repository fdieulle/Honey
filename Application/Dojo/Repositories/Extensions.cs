using Domain;
using Domain.Dtos.Workflows;
using Domain.Dtos;
using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo.Repositories
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

        public static JobViewModel ToViewModel(this JobDto dto) => new JobViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Status = dto.Status,
        };

        public static void Update(this JobViewModel vm, JobDto dto)
        {
            vm.Name = dto.Name;
            vm.Status = dto.Status;
        }

        public static void Update(this JobViewModel vm, JobDto dto, Dictionary<Guid, JobViewModel> jobs, Dictionary<Guid, RemoteTaskDto> tasks)
        {
            if (dto is SingleTaskJobDto sj) // This is a leaf
            {
                vm.Update(sj, tasks);
                return;
            }

            if (dto is ManyJobsDto mj)
            {
                vm.Type = mj.Behavior.ToString();
                vm.Host = "Branch";
                vm.Children.Clear();
                vm.Children.AddRange(mj.JobIds.Select(jobs.Get));
            }
        }

        private static void Update(this JobViewModel vm, SingleTaskJobDto dto, Dictionary<Guid, RemoteTaskDto> tasks)
        {
            vm.Type = dto.Parameters.Command;
            if (tasks.TryGetValue(dto.TaskId, out var task))
            {
                vm.Host = task.NinjaAddress;
                var ninjaState = task.NinjaState;
                if (ninjaState != null)
                {
                    vm.StartTime = ninjaState.StartTime;
                    vm.Progress = ninjaState.ProgressPercent;
                    vm.Duration = (ninjaState.IsFinal() ? ninjaState.EndTime : DateTime.Now) - ninjaState.StartTime;
                }
            }
        }

        private static JobViewModel Get(this Dictionary<Guid, JobViewModel> jobs, Guid id)
            => jobs.TryGetValue(id, out var job) ? job : JobViewModel.Empty;

        public static void UpdateTree(this JobViewModel vm)
        {
            var startedJobs = vm.Children.Where(p => p.StartTime.HasValue).ToList();
            if (startedJobs.Any())
            {
                foreach (var job in startedJobs)
                    job.UpdateTree();
                vm.StartTime = startedJobs.Min(p => p.StartTime);
                vm.Progress = startedJobs.Min(p => p.Progress);
                vm.Duration = startedJobs.Max(p => p.Duration);
            }
        }

        #endregion
    }
}
