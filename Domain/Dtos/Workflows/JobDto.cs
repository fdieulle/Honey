﻿using System;
using System.Text.Json.Serialization;

namespace Domain.Dtos.Workflows
{
    [JsonDerivedType(typeof(SingleTaskJobDto), typeDiscriminator: "single_task")]
    [JsonDerivedType(typeof(ManyJobsDto), typeDiscriminator: "many")]
    public class JobDto
    { 
        public Guid Id { get; set; }
        public string Name { get; set; }
        public JobStatus Status { get; set; }

        public override string ToString() => $"[{Id}] {Name} - {Status}";
    }

    [JsonDerivedType(typeof(SingleTaskJobParameters), typeDiscriminator: "single_task")]
    [JsonDerivedType(typeof(ManyJobsParameters), typeDiscriminator: "many")]
    public class JobParameters
    {
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
