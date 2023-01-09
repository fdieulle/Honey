using Domain.Dtos.Pipelines;
using System;

namespace Application.Dojo.Pipelines
{
    public class JobFactory : IJobFactory
    {
        private readonly Queue _queue;
        private readonly ITaskTracker _tracker;
        private readonly IDojoDb _db;

        public JobFactory(Queue queue, ITaskTracker tracker, IDojoDb db)
        {
            _queue = queue;
            _tracker = tracker;
            _db = db;
        }

        public IJob CreateJob(JobParameters parameters)
        {
            if (parameters is SingleTaskJobParameters sjp)
                return new SingleTaskJob(sjp, _queue, _tracker, _db);
            else if (parameters is ParallelJobsParameters pjp)
                return new ParallelJobs(pjp, this, _db);
            else if (parameters is LinkedJobsParameters ljp)
                return new LinkedJobs(ljp, this, _db);
            else
                throw new InvalidOperationException($"Job parameters: {parameters?.GetType()} is not supported");
        }

        public IJob CreateJob(JobDto dto)
        {
            if (dto is SingleTaskJobDto sjd)
                return new SingleTaskJob(sjd, _queue, _tracker, _db);
            else if (dto is ParallelJobsDto pjd)
                return new ParallelJobs(pjd, this, _db);
            else if (dto is LinkedJobsDto ljd)
                return new LinkedJobs(ljd, this, _db);
            else
                throw new InvalidOperationException($"Job {dto?.GetType()} is not supported");
        }
    }
}
