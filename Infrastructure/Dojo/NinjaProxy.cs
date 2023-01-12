using Application;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;

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
            => _client.GetAsync<List<TaskDto>>("Ninja/GetTasks").Result;

        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length) 
            => _client.GetAsync<List<TaskMessageDto>>(
                "Ninja/FetchMessages", 
                ("id", id.ToString()), 
                ("start", start.ToString()), 
                ("length", length.ToString())).Result;

        public Guid StartTask(string command, string arguments, int nbCores = 1) 
            => _client.PostAsArgsAsync<Guid>(
                "Ninja/StartTask", 
                ("command", command), 
                ("arguments", arguments), 
                ("nbCores", nbCores.ToString())).Result;

        public void CancelTask(Guid id) 
            => _client.PostAsArgsAsync("Ninja/CancelTask", ("id", id.ToString())).Wait();

        public void DeleteTask(Guid id) 
            => _client.DeleteAsArgsAsync("Ninja/DeleteTask", ("id", id.ToString())).Wait();

        public NinjaResourcesDto GetResources() 
            => _client.GetAsync<NinjaResourcesDto>("Ninja/GetResources").Result;
    }
}
