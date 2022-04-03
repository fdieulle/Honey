using Domain;
using Domain.Dtos;
using System;

namespace Domain.Entities
{
    public class TaskEntity
    {
        public Guid Id { get; set; }

        public string Command { get; set; }

        public string Arguments { get; set; }

        public int Pid { get; set; }

        public DateTime StartTime { get; set; }

        public TaskStatus Status { get; set; }

        public DateTime EndTime { get; set; }

        public string WorkingFolder { get; set; }
    }
}
