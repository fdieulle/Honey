using System;
using System.Text.Json.Serialization;

namespace Domain.Dtos.Pipelines
{
    [JsonDerivedType(typeof(SingleTaskJobDto), typeDiscriminator: "single_task")]
    [JsonDerivedType(typeof(ParallelJobsDto), typeDiscriminator: "parallel")]
    [JsonDerivedType(typeof(LinkedJobsDto), typeDiscriminator: "linked")]
    public class JobDto
    { 
        public Guid Id { get; set; }
        public string Name { get; set; }
        public JobStatus Status { get; set; }
    }

    [JsonDerivedType(typeof(SingleTaskJobParameters), typeDiscriminator: "single_task")]
    [JsonDerivedType(typeof(ParallelJobsParameters), typeDiscriminator: "parallel")]
    [JsonDerivedType(typeof(LinkedJobsParameters), typeDiscriminator: "linked")]
    public class JobParameters
    {
        public string Name { get; set; }
    }
}
