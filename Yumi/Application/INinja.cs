using System;
using System.Collections.Generic;

namespace Yumi.Application
{
    public interface INinja
    {
        IEnumerable<Job> GetJobs();

        IEnumerable<JobMessage> FetchMessages(Guid id, int start, int length);

        Guid StartJob(string command, string arguments, int nbCores = 1);

        void CancelJob(Guid id);

        void DeleteJob(Guid id);

        NinjaResources GetResources();
    }
}
