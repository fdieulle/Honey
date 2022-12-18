﻿using System;

namespace Domain.Dtos
{
    public enum QueuedTaskStatus
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

        public string NinjaAddress { get; set; }

        public string QueueName { get; set; }

        public StartTaskDto StartTask { get; set; }

        public QueuedTaskStatus Status { get; set; }

        public ulong Order { get; set; }

        public TaskDto NinjaState { get; set; } = new TaskDto { Status = TaskStatus.Pending };

        public RemoteTaskDto() { Id = Guid.NewGuid(); }

        public RemoteTaskDto(string queue, string name, StartTaskDto task, ulong order)
        {
            Id = Guid.NewGuid();
            Name = name ?? Id.ToString();
            QueueName = queue;
            StartTask = task;
            Order = order;
        }
    }
}