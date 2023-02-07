using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Colony.Workflows
{

    public class ParallelJobs : ManyJobs
    {
        public ParallelJobs(ManyJobsParameters parameters, IJobFactory factory, IColonyDb db)
            : base(parameters, factory, db)
        {
            
        }

        public ParallelJobs(ManyJobsDto dto, IJobFactory factory, IColonyDb db)
            : base(dto, factory, db)
        {
            // Todo: Handle errors

            // Todo: Handle colony down time and status changes during the down time
        }

        protected override void Start(IEnumerable<IJob> jobs)
        {
            foreach (var job in jobs)
                job.Start();
        }

    }
}
