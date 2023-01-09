using Domain.Dtos.Pipelines;
using System;

namespace Application.Dojo.Pipelines
{
    public interface IJob : IDisposable
    {
        event Action<IJob> Updated;
        Guid Id { get; }
        JobStatus Status { get; }
        void Start();
        void Cancel();
        void Delete();
    }
}
