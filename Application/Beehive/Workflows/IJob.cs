﻿using Domain.Dtos.Workflows;
using System;

namespace Application.Beehive.Workflows
{
    public interface IJob : IDisposable
    {
        event Action<IJob> Updated;
        Guid Id { get; }
        JobStatus Status { get; }
        JobDto Dto { get; }
        void Start();
        void Cancel();
        void Recover();
        void Delete();
    }
}