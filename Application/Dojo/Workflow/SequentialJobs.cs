using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo.Workflows
{
    public class SequentialJobs : ManyJobs
    {
        public SequentialJobs(ManyJobsParameters parameters, IJobFactory factory, IDojoDb db) 
            : base(parameters, factory, db)
        {
        }

        public SequentialJobs(ManyJobsDto dto, IJobFactory factory, IDojoDb db) 
            : base(dto, factory, db)
        {
            // Todo: Handle errors

            // Todo: Handle dojo down time and status changes during the down time
        }

        protected override void Start(IEnumerable<IJob> jobs)
        {
            Start(Jobs.FirstOrDefault());
        }

        private void Start(IJob job)
        {
            if (job == null) return;
            
            job.Updated += OnJobUpdated;
            job.Start();
        }

        private void OnJobUpdated(IJob job)
        {
            if (job.Status.IsFinal())
                job.Updated -= OnJobUpdated;

            if (job.Status == JobStatus.Completed)
                Start(GetNextJob(job));
        }

        private IJob GetNextJob(IJob job)
        {
            for(var i=0; i<Jobs.Length; i++)
            {
                if (job.Id != Jobs[i].Id)
                    continue;
                for (var j=i+1; j< Jobs.Length; j++)
                {
                    if (Jobs[j].CanStart())
                        return Jobs[j];
                }
            }

            return null;
        }
    }
}
