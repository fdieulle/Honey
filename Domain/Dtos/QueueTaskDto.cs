using System;

namespace Domain.Dtos
{
    public class QueueTaskDto
    {
        public Guid Id { get; set; }

        public string NinjaAddress { get; set; }

        public string QueueName { get; set; }

        public StartTaskDto StartTask { get; set; }

        public TaskDto NinjaState { get; set; }

        public QueueTaskDto() { Id = Guid.NewGuid(); }

        public QueueTaskDto(string queue, StartTaskDto task)
        {
            Id = Guid.NewGuid();
            QueueName = queue;
            StartTask = task;
            NinjaState = new TaskDto { Status = TaskStatus.Pending };
        }
    }
}
