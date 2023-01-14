﻿using System;

namespace Domain.Dtos.Pipelines
{
    public class SingleTaskJobDto : JobDto
    {
        public Guid TaskId { get; set; }

        public TaskParameters Parameters { get; set; }
    }

    public class SingleTaskJobParameters : JobParameters
    {
        public TaskParameters StartTask { get; set; }
    }
}
