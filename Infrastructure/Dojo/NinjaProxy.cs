using Application;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Infrastructure.Dojo
{
    internal class NinjaProxy : NinjaClient, INinja
    {
        public NinjaProxy(string address) 
            : base(address) { }

        public IEnumerable<TaskDto> GetTasks()
        {
            return Client.GetAsync<List<TaskDto>>("Ninja/GetTasks").Result;
        }

        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length)
        {
            throw new NotImplementedException();
        }

        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            return Client.PostAsArgsAsync<Guid>("Ninja/StartTask", ("command", command), ("arguments", arguments), ("nbCores", nbCores.ToString())).Result;
        }

        public void CancelTask(Guid id)
        {
            Client.PostAsArgsAsync("Ninja/CancelTask", ("id", id.ToString())).Wait();
        }

        public void DeleteTask(Guid id)
        {
            Client.DeleteAsArgsAsync("Ninja/DeleteTask", ("id", id.ToString())).Wait();
        }

        public NinjaResourcesDto GetResources()
        {
            return Client.GetAsync<NinjaResourcesDto>("Ninja/GetResources").Result;
        }
    }
}
