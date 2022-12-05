using Domain.Dtos;
using System;

namespace Domain.Entities
{
    public class QueuedTaskEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string NinjaAddress { get; set; }

        public string QueueName { get; set; }

        public string Command { get; set; }
        public string Arguments { get; set; }
        public int NbCores { get; set; } = 1;

        public QueuedTaskStatus Status { get; set; }

        public ulong Order { get; set; }
    }
}
