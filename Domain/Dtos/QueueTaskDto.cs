using System;

namespace Domain.Dtos
{
    public class QueueTaskDto
    {
        public Guid Id { get; set; }

        public string NinjaAddress { get; set; }

        public StartTaskDto StartTask { get; set; }

        public TaskDto NinjaState { get; set; }

        public QueueTaskDto() { Id = Guid.NewGuid(); }

        public QueueTaskDto(StartTaskDto task)
        {
            Id = Guid.NewGuid();
            StartTask = task;
            NinjaState = new TaskDto { Status = TaskStatus.Pending };
        }
    }
}
