﻿using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;

namespace Application.Colony.Workflows
{
    public class Workflow
    {
        private readonly IJob _rootJob;
        private readonly IDispatcher _sequencer;
        private readonly IColonyDb _db;

        public event Action<Workflow> Deleted;

        public Guid Id => Dto.Id;
        public WorkflowDto Dto { get; }
        public Workflow(WorkflowParameters parameters, Beehive beehive, ITaskTracker tracker, IDispatcherFactory dispatcherFactory, IColonyDb db)
        {
            _db = db;
            Dto = new WorkflowDto
            {
                Id = Guid.NewGuid(),
                Name = parameters.Name,
                Beehive = parameters.Beehive,
                Owner = parameters.Owner,
            };
            _sequencer = dispatcherFactory.CreateSequencer(Dto.Id.ToString("N"));

            var factory = new JobFactory(beehive, tracker.CreateScope(_sequencer), _db);

            _rootJob = factory.CreateJob(parameters.RootJob);
            Dto.RootJobId = _rootJob.Id;

            db.CreateWorkflow(Dto);

            _rootJob.Updated += OnJobUpdate;
        }

        public Workflow(WorkflowDto dto, Beehive beehive, ITaskTracker tracker, IDispatcherFactory dispatcherFactory, IColonyDb db)
        {
            Dto = dto;
            _sequencer = dispatcherFactory.CreateSequencer(Dto.Id.ToString("N"));
            _db = db;

            var factory = new JobFactory(beehive, tracker.CreateScope(_sequencer), _db);

            var jobDto = db.FetchJob(dto.RootJobId);
            if (jobDto != null)
            {
                _rootJob = factory.CreateJob(jobDto);
                _rootJob.Updated += OnJobUpdate;
            }
            else
            {
                // Todo: Generate in error job
            }
        }

        public void Start() 
        {
            _sequencer.Dispatch(() =>
            {
                if (_rootJob.CanStart())
                    _rootJob.Start();
            });
        }

        public void Cancel() 
        {
            _sequencer.Dispatch(() =>
            {
                if (_rootJob.CanCancel())
                    _rootJob.Cancel();
            });
        }

        public void Recover()
        {
            _sequencer.Dispatch(() =>
            {
                if (_rootJob.CanRecover())
                    _rootJob.Recover();
            });
        }

        public void Delete() 
        {
            _sequencer.Dispatch(() =>
            {
                if (_rootJob.CanDelete())
                    _rootJob.Delete();
            });
        }

        public IEnumerable<JobDto> GetJobs()
        {
            if (_rootJob == null) yield break;

            var stack = new Stack<IJob>();
            stack.Push(_rootJob);
            while(stack.Count > 0)
            {
                var job = stack.Pop();
                yield return job.Dto;

                if (job is ManyJobs mj)
                {
                    foreach (var j in mj.Jobs)
                        stack.Push(j);
                }
            }
        }

        private void OnJobUpdate(IJob job)
        {
            if (job.Status != JobStatus.Deleted)
                return;
            
            job.Updated -= OnJobUpdate;
            _db.DeleteWorkflow(Id);

            Deleted?.Invoke(this);
        }
    }
}
