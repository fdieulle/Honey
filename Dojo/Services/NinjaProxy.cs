using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Yumi;
using Yumi.Application;
using Yumi.Domain;

namespace Dojo.Services
{
    public class NinjaProxy : INinja
    {
        private readonly HttpClient _client = new HttpClient();
        public NinjaProxy(string address)
        {
            _client.BaseAddress = new Uri(address);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public IEnumerable<Job> GetJobs()
        {
            return _client.GetAsync<List<Job>>("Ninja/GetJobs").Result;
        }

        public IEnumerable<JobMessage> FetchMessages(Guid id, int start, int length)
        {
            throw new NotImplementedException();
        }

        public Guid StartJob(string command, string arguments, int nbCores = 1)
        {
            return _client.PostAsJsonAsync<StartJob, Guid>("Ninja/StartJob", new StartJob { Command = command, Arguments = arguments, NbCores = nbCores }).Result;
        }

        public void CancelJob(Guid id)
        {
            _client.PostAsJsonAsync("Ninja/CancelJob", id).Wait();
        }

        public void DeleteJob(Guid id)
        {
            _client.DeleteAsJsonAsync("Ninja/CancelJob", ("id", id.ToString())).Wait();
        }

        public NinjaResources GetResources()
        {
            return _client.GetAsync<NinjaResources>("Ninja/GetResources").Result;
        }

        
    }
}
