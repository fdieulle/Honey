using System;
using System.Collections.Generic;

namespace Domain.Dtos
{
    public enum RemoteTaskStatus
    {
        Pending,
        Running,
        Completed,
        CancelRequested,
        CancelPending,
        Cancel,
        Error,
        Deleted
    }

    public class RemoteTaskDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string BeeAddress { get; set; }

        public string Beehive { get; set; }

        public TaskParameters Parameters { get; set; }

        public RemoteTaskStatus Status { get; set; }

        public ulong Order { get; set; }

        public TaskDto BeeState { get; set; } = new TaskDto { Status = TaskStatus.Pending };

        public List<TaskMessageDto> Messages { get; set; } = new List<TaskMessageDto>();

        public RemoteTaskDto() { Id = Guid.NewGuid(); }

        public RemoteTaskDto(string beehive, string name, TaskParameters task, ulong order)
        {
            Id = Guid.NewGuid();
            Name = name ?? Id.ToString();
            Beehive = beehive;
            Parameters = task;
            Order = order;
        }
    }
}
