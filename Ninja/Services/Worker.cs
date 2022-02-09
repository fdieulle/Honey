using Ninja.Dto;
using System;
using System.Collections.Generic;

namespace Ninja.Services
{
    public class Worker
    {
        public IEnumerable<Job> GetJobs()
        {
            throw new NotImplementedException();
        }

        public string StartJob(string name, string command, string arguments, object nbCore)
        {
            throw new NotImplementedException();
        }

        public void CancelJob(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteJob(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<JobMessage> FetchMessages(string id, int start, int length)
        {
            throw new NotImplementedException();
        }
    }
}
