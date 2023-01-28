using Domain.Dtos.Workflows;
using Domain.Dtos;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Xunit;
using System.Text.Json;

namespace Application.Tests.Beehive
{
    public class WorkflowTests
    {
        [Fact]
        public void MapReduceTest()
        {
            var mapper = new ManyJobsParameters
            {
                Name = "Mapper",
                Behavior = JobsBehavior.Parallel,
                Jobs = new JobParameters[]
                {
                    new SingleTaskJobParameters
                    {
                        Name = "Map 1",
                        Task = new TaskParameters
                        {
                            Command = "cmd map 1"
                        }
                    },
                    new SingleTaskJobParameters
                    {
                        Name = "Map 2",
                        Task = new TaskParameters
                        {
                            Command = "cmd map 2"
                        }
                    },
                    new SingleTaskJobParameters
                    {
                        Name = "Map 3",
                        Task = new TaskParameters
                        {
                            Command = "cmd map 3"
                        }
                    }
                },
            };

            var reducer = new SingleTaskJobParameters
            {
                Name = "Reducer",
                Task = new TaskParameters
                {
                    Command = "cmd reduce"
                }
            };

            var mapReduce = new ManyJobsParameters
            {
                Name = "MapReduce",
                Behavior = JobsBehavior.Sequential,
                Jobs = new JobParameters[] {mapper, reducer },
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize<JobParameters>(mapReduce, options);

            var parameters = System.Text.Json.JsonSerializer.Deserialize<JobParameters>(json);

            Assert.NotNull(parameters);
            Assert.IsAssignableFrom<ManyJobsParameters>(parameters);
        }
    }
}
