using Domain.Dtos.Workflows;
using System;

namespace Application.Beehive.Workflows
{
    public class JobFactory : IJobFactory
    {
        private readonly Colony _colony;
        private readonly ITaskTracker _tracker;
        private readonly IBeehiveDb _db;

        public JobFactory(Colony colony, ITaskTracker tracker, IBeehiveDb db)
        {
            _colony = colony;
            _tracker = tracker;
            _db = db;
        }

        public IJob CreateJob(JobParameters parameters)
        {
            if (parameters is SingleTaskJobParameters sj)
                return new SingleTaskJob(sj, _colony, _tracker, _db);
            else if (parameters is ManyJobsParameters mj)
            {
                switch (mj.Behavior)
                {
                    case JobsBehavior.Parallel:
                        return new ParallelJobs(mj, this, _db);
                    case JobsBehavior.Sequential:
                        return new SequentialJobs(mj, this, _db);
                }
            }
            
            throw new InvalidOperationException($"Job parameters: {parameters} is not supported");
        }

        public IJob CreateJob(JobDto dto)
        {
            if (dto is SingleTaskJobDto sj)
                return new SingleTaskJob(sj, _colony, _tracker, _db);
            else if (dto is ManyJobsDto mj)
            {
                switch (mj.Behavior)
                {
                    case JobsBehavior.Parallel:
                        return new ParallelJobs(mj, this, _db);
                    case JobsBehavior.Sequential:
                        return new SequentialJobs(mj, this, _db);
                }
            }

            throw new InvalidOperationException($"Job {dto} is not supported");
        }
    }
}
