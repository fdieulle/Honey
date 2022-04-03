using Application;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Infrastructure.Dojo
{
    internal class NinjaProxy : INinja
    {
        private readonly HttpClient _client = new HttpClient();
        public NinjaProxy(string address)
        {
            _client.BaseAddress = new Uri(address);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public IEnumerable<TaskDto> GetTasks()
        {
            return _client.GetAsync<List<TaskDto>>("Ninja/GetTasks").Result;
        }

        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length)
        {
            throw new NotImplementedException();
        }

        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            return _client.PostAsJsonAsync<StartTaskDto, Guid>("Ninja/StartTask", new StartTaskDto { Command = command, Arguments = arguments, NbCores = nbCores }).Result;
        }

        public void CancelTask(Guid id)
        {
            _client.PostAsJsonAsync("Ninja/CancelTask", id).Wait();
        }

        public void DeleteTask(Guid id)
        {
            _client.DeleteAsJsonAsync("Ninja/DeleteTask", ("id", id.ToString())).Wait();
        }

        public NinjaResourcesDto GetResources()
        {
            return _client.GetAsync<NinjaResourcesDto>("Ninja/GetResources").Result;
        }
    }
}
