﻿using Application;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;

namespace Infrastructure.Colony
{
    internal class BeeProxy : IBee
    {
        private readonly HttpClient _client = new HttpClient();

        public BeeProxy(string address)
        {
            _client.BaseAddress = new Uri(address);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public IEnumerable<TaskDto> GetTasks() 
            => _client.GetAsync<List<TaskDto>>("Bee/GetTasks").Result;

        public async Task<List<string>> FetchLogsAsync(Guid id, int start = 0, int length = -1)
            => await _client.GetAsync<List<string>>(
                "Bee/FetchMessages", ("id", id.ToString()), ("start", start.ToString()), ("length", length.ToString()));

        public Guid StartTask(string command, string arguments, int nbCores = 1) 
            => _client.PostAsArgsAsync<Guid>(
                "Bee/StartTask", 
                ("command", command), 
                ("arguments", arguments), 
                ("nbCores", nbCores.ToString())).Result;

        public async void CancelTask(Guid id) 
            => await _client.PostAsArgsAsync("Bee/CancelTask", ("id", id.ToString()));

        public async void DeleteTask(Guid id) 
            => await _client.DeleteAsArgsAsync("Bee/DeleteTask", ("id", id.ToString()));

        public BeeResourcesDto GetResources() 
            => _client.GetAsync<BeeResourcesDto>("Bee/GetResources").Result;
    }
}
