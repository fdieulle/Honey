﻿using System;

namespace Domain.Dtos
{
    public enum TaskStatus
    {
        Pending,
        Running,
        Done,
        Cancel,
        Error,
        EndedWithoutSupervision,
    }

    public class TaskDto
    {
        public Guid Id { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime ExpectedEndTime { get; set; }
        public double Progress { get; set; }
    }
}