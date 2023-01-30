using Domain.Dtos.Workflows;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive.Workflows
{
    public class SequentialJobs : ManyJobs
    {
        public SequentialJobs(ManyJobsParameters parameters, IJobFactory factory, IBeehiveDb db) 
            : base(parameters, factory, db)
        {
        }

        public SequentialJobs(ManyJobsDto dto, IJobFactory factory, IBeehiveDb db) 
            : base(dto, factory, db)
        {
            // Todo: Handle errors

            // Todo: Handle beehive down time and status changes during the down time
        }

        protected override void Start(IEnumerable<IJob> jobs) 
            => Start(Jobs.FirstOrDefault());

        private void Start(IJob job)
        {
            if (job == null) return;
            
            job.Updated += OnJobUpdated;
            job.Start();
        }

        private void OnJobUpdated(IJob job)
        {
            if (job.IsFinalStatus())
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
