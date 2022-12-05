﻿using System;

namespace Domain.Dtos
{
    public enum QueuedTaskStatus
    {
        Unknwon, // When no ninja status has been received yet
        Pending,
        Running,
        CancelRequested,
        Completed
    }

    public class QueuedTaskDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string NinjaAddress { get; set; }

        public string QueueName { get; set; }

        public StartTaskDto StartTask { get; set; }

        public QueuedTaskStatus Status { get; set; }

        public ulong Order { get; set; }

        public TaskDto NinjaState { get; set; }

        public QueuedTaskDto() { Id = Guid.NewGuid(); }

        public QueuedTaskDto(string queue, string name, StartTaskDto task, ulong order)
        {
            Id = Guid.NewGuid();
            Name = name ?? Id.ToString();
            QueueName = queue;
            StartTask = task;
            NinjaState = new TaskDto { Status = TaskStatus.Pending };
            Order = order;
        }
    }
}
