using System;

namespace Domain.Dtos
{
    public class PendingTaskDto
    {
        public Guid Id { get; set; }

        public StartTaskDto Task { get; set; }

        public PendingTask() { }

        public PendingTask(StartTaskDto task)
        {
            Id = Guid.NewGuid();
            Task = task;
        }
    }
}
