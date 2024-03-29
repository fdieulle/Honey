﻿using Domain.Dtos.Workflows;
using System;

namespace Application.Colony.Workflows
{
    public abstract class Job<TParameters, TDto> : IJob
        where TParameters : JobParameters
        where TDto : JobDto, new()
    {
        private bool _isDisposed;

        public Guid Id => Dto.Id;

        public string Name => Dto.Name;

        public JobStatus Status => Dto.Status;

        JobDto IJob.Dto => Dto;
        public TDto Dto { get; } = new TDto();

        protected IColonyDb Db { get; }

        public event Action<IJob> Updated;

        protected Job(TParameters parameters, IColonyDb db)
        {
            Dto = new TDto() 
            { 
                Id = Guid.NewGuid(), 
                Name = parameters.Name,
                Status = JobStatus.Pending 
            };

            Db = db;
        }

        protected Job(TDto dto, IColonyDb db)
        {
            Dto = dto;
            Db = db;
        }

        public abstract void Start();

        public abstract void Cancel();

        public abstract void Recover();

        public abstract void Delete();

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            OnDispose();
        }

        protected abstract void OnDispose();

        protected void Update(JobStatus status)
        {
            if (Status == status) return;
            Dto.Status = status;

            Db.UpdateJob(Dto); // Todo: should I clone the Dto here ?
            Updated?.Invoke(this);
        }

        public override string ToString() => $"[{Name}] {Status} - {Id}";
    }
}
