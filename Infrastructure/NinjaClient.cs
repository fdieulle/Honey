using System;
using System.Net.Http.Headers;
using System.Net.Http;
using Domain.Dtos;
using Infrastructure.Dojo;

namespace Infrastructure
{
    public class NinjaClient
    {
        protected HttpClient Client { get; } = new HttpClient();
        public NinjaClient(string address)
        {
            Client.BaseAddress = new Uri(address);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void UpdateTask(TaskStateDto dto)
        {
            Client.PostAsJsonAsync("Ninja/UpdateTask", dto).Wait();
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
