﻿using Domain;
using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Colony.Workflows
{
    public abstract class ManyJobs: Job<ManyJobsParameters, ManyJobsDto>
    {
        public IJob[] Jobs { get; }

        public ManyJobs(ManyJobsParameters parameters, IJobFactory factory, IColonyDb db)
            : base(parameters, db)
        {
            var jobs = parameters.Jobs ?? Array.Empty<JobParameters>();
            Jobs = jobs.Select(p => factory.CreateJob(p)).ToArray();

            Dto.JobIds = Jobs.Select(p => p.Id).ToArray();
            Dto.Behavior = parameters.Behavior;

            db.CreateJob(Dto);

            foreach (var job in Jobs) // TODO: Should we subscribe during the Start phase to avoid useless updates from previous job dependencies
                job.Updated += OnUpdated;
        }

        public ManyJobs(ManyJobsDto dto, IJobFactory factory, IColonyDb db)
            : base(dto, db)
        {
            Jobs = dto.JobIds
                .Select(id => factory.CreateJob(db.FetchJob(id)))
                .ToArray();

            foreach (var job in Jobs) // TODO: Should we subscribe during the Start phase to avoid useless updates from previous job dependencies
                job.Updated += OnUpdated;
        }

        public override sealed void Start()
        {
            if (Jobs.Length == 0)
            {
                Update(JobStatus.Completed);
                return;
            }

            if (!Jobs.Any(s => s.CanStart()))
                return;

            Update(JobStatus.Running);

            Start(Jobs.Where(p => p.CanStart()));
        }

        protected abstract void Start(IEnumerable<IJob> jobs);

        public override void Cancel()
        {
            if (Jobs.Length == 0)
            {
                Update(JobStatus.Cancel);
                return;
            }

            if (!Jobs.Any(s => s.CanCancel()))
                return;

            Update(JobStatus.CancelRequested);

            foreach (var job in Jobs.Where(p => p.CanCancel()))
                job.Cancel();
        }

        public override void Recover()
        {
            if (Jobs.Length == 0)
                return;

            if (!Jobs.Any(s => s.CanRecover()))
                return;

            Update(JobStatus.Running);

            foreach (var job in Jobs.Where(p => p.CanRecover()))
                job.Recover();
        }

        public override void Delete()
        {
            if (Jobs.Length == 0)
            {
                Update(JobStatus.Deleted);
                return;
            }

            if (!Jobs.Any(s => s.CanDelete()))
                return;

            Update(JobStatus.DeleteRequested);

            foreach (var job in Jobs.Where(p => p.CanDelete()))
                job.Delete();
        }

        private void OnUpdated(IJob job)
        {
            var status = Jobs.GetStatus();
            if (!status.IsFinal())
            {
                if (Dto.Status == JobStatus.CancelRequested)
                    return;
            }

            Update(status);
        }

        protected override sealed void OnDispose()
        {
            foreach (var job in Jobs)
            {
                job.Updated -= OnUpdated;
                job.Dispose();
            }
        }
    }

    public static class ManyJobsExtensions
    {
        public static JobStatus GetStatus(this IJob[] jobs)
        {
            if (jobs.Length == 0 || jobs.All(s => s.Status == JobStatus.Completed))
                return JobStatus.Completed;

            if (jobs.All(s => s.Status == JobStatus.Pending))
                return JobStatus.Pending;

            if (jobs.All(s => s.Status == JobStatus.Deleted))
                return JobStatus.Deleted;

            if (jobs.Any(s => s.Status == JobStatus.Error))
                return JobStatus.Error;

            if (jobs.All(s => s.Status.IsFinal()) && jobs.Any(s => s.Status == JobStatus.Cancel))
                return JobStatus.Cancel;

            if (jobs.Any(s => s.Status == JobStatus.DeleteRequested))
                return JobStatus.DeleteRequested;

            return JobStatus.Running;
        }
    }
}
