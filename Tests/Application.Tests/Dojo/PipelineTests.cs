using Domain.Dtos.Pipelines;
using Domain.Dtos;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Xunit;
using System.Text.Json;

namespace Application.Tests.Dojo
{
    public class PipelineTests
    {
        [Fact]
        public void MapReduceTest()
        {
            var mapper = new ParallelJobsParameters
            {
                Name = "Mapper",
                Jobs = new JobParameters[]
                {
                    new SingleTaskJobParameters
                    {
                        Name = "Map 1",
                        StartTask = new TaskParameters
                        {
                            Command = "cmd map 1"
                        }
                    },
                    new SingleTaskJobParameters
                    {
                        Name = "Map 2",
                        StartTask = new TaskParameters
                        {
                            Command = "cmd map 2"
                        }
                    },
                    new SingleTaskJobParameters
                    {
                        Name = "Map 3",
                        StartTask = new TaskParameters
                        {
                            Command = "cmd map 3"
                        }
                    }
                },
            };

            var reducer = new SingleTaskJobParameters
            {
                Name = "Reducer",
                StartTask = new TaskParameters
                {
                    Command = "cmd reduce"
                }
            };

            var mapReduce = new LinkedJobsParameters
            {
                Name = "MapReduce",
                JobA = mapper,
                JobB = reducer,
                LinkType = LinkedJobType.FinishToStart
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize<JobParameters>(mapReduce, options);

            var parameters = System.Text.Json.JsonSerializer.Deserialize<JobParameters>(json);

            Assert.NotNull(parameters);
            Assert.IsAssignableFrom<LinkedJobsParameters>(parameters);
        }
    }
}
