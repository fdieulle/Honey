using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive.Workflows
{

    public class ParallelJobs : ManyJobs
    {
        public ParallelJobs(ManyJobsParameters parameters, IJobFactory factory, IBeehiveDb db)
            : base(parameters, factory, db)
        {
            
        }

        public ParallelJobs(ManyJobsDto dto, IJobFactory factory, IBeehiveDb db)
            : base(dto, factory, db)
        {
            // Todo: Handle errors

            // Todo: Handle beehive down time and status changes during the down time
        }

        protected override void Start(IEnumerable<IJob> jobs)
        {
            foreach (var job in jobs)
                job.Start();
        }

    }
}
