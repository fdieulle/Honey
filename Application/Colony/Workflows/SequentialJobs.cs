using Domain.Dtos.Workflows;
using System.Collections.Generic;
using System.Linq;

namespace Application.Colony.Workflows
{
    public class SequentialJobs : ManyJobs
    {
        public SequentialJobs(ManyJobsParameters parameters, IJobFactory factory, IColonyDb db) 
            : base(parameters, factory, db)
        {
        }

        public SequentialJobs(ManyJobsDto dto, IJobFactory factory, IColonyDb db) 
            : base(dto, factory, db)
        {
            // Todo: Handle errors

            // Todo: Handle colony down time and status changes during the down time
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
                Start(Jobs.FirstOrDefault(p => p.CanStart()));
        }

        public override void Recover()
        {
            Recover(Jobs.FirstOrDefault(p => p.CanRecover()));
        }

        private void Recover(IJob job)
        {
            if (job == null) return;

            job.Updated += OnJobUpdated;
            job.Recover();
        }
    }
}
