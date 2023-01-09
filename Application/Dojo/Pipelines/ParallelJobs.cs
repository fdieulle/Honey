using Domain.Dtos.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo.Pipelines
{
    public class ParallelJobs : Job<ParallelJobsParameters, ParallelJobsDto>
    {
        private readonly IJob[] _jobs;

        public ParallelJobs(ParallelJobsParameters parameters, IJobFactory factory, IDojoDb db)
            : base(parameters, db)
        {
            var jobs = parameters.Jobs ?? Array.Empty<JobParameters>();
            _jobs = jobs.Select(p => factory.CreateJob(p)).ToArray();

            db.CreateJob(Dto);

            foreach (var job in _jobs) // TODO: Should we subscribe during the Start phase to avoid useless updates from previous job dependencies
                job.Updated += OnUpdated;
        }

        public ParallelJobs(ParallelJobsDto dto, IJobFactory factory, IDojoDb db)
            : base(dto, db)
        {
            _jobs = dto.JobIds
                .Select(id => factory.CreateJob(db.FetchJob(id)))
                .ToArray();

            // Todo: Handle errors

            foreach (var job in _jobs) // TODO: Should we subscribe during the Start phase to avoid useless updates from previous job dependencies
                job.Updated += OnUpdated;

            // Todo: Handle dojo down time and status changes during the down time
        }

        public override void Start()
        {
            if (None(s => s.CanStart()))
                return;

            foreach (var job in _jobs.Where(p => p.Status.CanStart()))
                job.Start();
        }

        public override void Cancel()
        {
            if (None(s => s.CanCancel()))
                return;

            Update(JobStatus.CancelRequested);
            foreach (var job in _jobs.Where(p => p.Status.CanCancel()))
                job.Cancel();
        }

        public override void Delete()
        {
            if (None(s => s.CanDelete()))
                return;

            foreach (var job in _jobs.Where(p => p.Status.CanDelete()))
                job.Delete();
        }

        private void OnUpdated(IJob job)
        {
            if (All(s => s == JobStatus.Completed))
            {
                Update(JobStatus.Completed);
                return;
            }
            if (All(s => s == JobStatus.Pending))
            {
                Update(JobStatus.Pending);
                return;
            }
            if (Any(s => s == JobStatus.Error))
            {
                Update(JobStatus.Error);
                return;
            }
            if (All(s => s == JobStatus.Deleted))
            {
                Update(JobStatus.Deleted);
                return;
            }
            if (All(s => s.IsFinal()) && Any(s => s == JobStatus.Cancel))
            {
                Update(JobStatus.Cancel);
                return;
            }

            Update(JobStatus.Running);
        }

        protected override void OnDispose()
        {
            foreach (var job in _jobs)
            {
                job.Updated -= OnUpdated;
                job.Dispose();
            }
        }

        private bool All(Predicate<JobStatus> predicate) => _jobs.All(p => predicate(p.Status));
        private bool Any(Predicate<JobStatus> predicate) => _jobs.Any(p => predicate(p.Status));
        private bool None(Predicate<JobStatus> predicate) => !All(predicate);
    }
}
