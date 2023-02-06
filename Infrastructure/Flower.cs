using System;
using System.Net.Http.Headers;
using System.Net.Http;
using Domain.Dtos;
using Infrastructure.Beehive;
using Application;

namespace Infrastructure
{
    public class Flower : IFlower
    {
        private readonly HttpClient _client = new HttpClient();
        public Flower(string address)
        {
            _client.BaseAddress = new Uri(address);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void UpdateTask(TaskStateDto dto)
        {
            _client.PostAsJsonAsync("Flower/UpdateTask", dto).Wait();
        }

        public void UpdateTask(Guid taskId, double progressPercent, DateTime expectedEndTime = default, string message = null)
        {
            UpdateTask(new TaskStateDto
            {
                TaskId = taskId,
                ProgressPercent = progressPercent,
                ExpectedEndTime = expectedEndTime,
                Message = message
            });
        }
    }
}
