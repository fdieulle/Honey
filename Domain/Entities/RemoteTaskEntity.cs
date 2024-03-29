﻿using Domain.Dtos;
using System;

namespace Domain.Entities
{
    public class RemoteTaskEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string BeeAddress { get; set; }

        public string Beehive { get; set; }

        public string Command { get; set; }
        public string Arguments { get; set; }
        public int NbCores { get; set; } = 1;

        public RemoteTaskStatus Status { get; set; }

        public ulong Order { get; set; }

        public Guid BeeTaskId { get; set; }
    }
}
