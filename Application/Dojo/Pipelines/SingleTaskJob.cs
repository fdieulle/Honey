using Domain.Dtos;
using Domain.Dtos.Pipelines;
using System;

namespace Application.Dojo.Pipelines
{
    public class SingleTaskJob : Job<SingleTaskJobParameters, SingleTaskJobDto>
    {
        private readonly Queue _queue;
        private readonly ITaskTracker _tracker;
        private IDisposable _subscription;

        public SingleTaskJob(SingleTaskJobParameters parameters, Queue queue, ITaskTracker tracker, IDojoDb db)
            : base(parameters, db)
        {
            Dto.StartTask = parameters.StartTask ?? new TaskParameters();
            _queue = queue;
            _tracker = tracker;

            db.CreateJob(Dto);
        }

        public SingleTaskJob(SingleTaskJobDto dto, Queue queue, ITaskTracker tracker, IDojoDb db)
            : base(dto, db) 
        {
            _queue = queue;
            _tracker = tracker;

            if (dto.TaskId != Guid.Empty)
                _subscription = tracker.Subscribe(dto.TaskId, OnTaskUpdated);

            // Todo: Handle dojo down time and status changes during the down time
        }

        public override void Start()
        {
            if (!Status.CanStart()) return;

            Dto.TaskId = _queue.StartTask(Dto.Name, Dto.StartTask);
            _subscription = _tracker.Subscribe(Dto.TaskId, OnTaskUpdated);
        }

        public override void Cancel()
        {
            if (!Status.CanCancel()) return;

            Update(JobStatus.CancelRequested);
            _queue.CancelTask(Dto.TaskId);
        }

        public override void Delete()
        {
            if (!Status.CanDelete()) return;

            _queue.DeleteTask(Dto.TaskId);
        }

        private void OnTaskUpdated(RemoteTaskDto dto)
        {
            switch (dto.Status)
            {
                case RemoteTaskStatus.Pending:
                    Update(JobStatus.Pending);
                    break;
                case RemoteTaskStatus.Running:
                    Update(JobStatus.Running);
                    break;
                case RemoteTaskStatus.Completed:
                    Update(JobStatus.Completed);
                    break;
                case RemoteTaskStatus.CancelRequested:
                    Update(JobStatus.CancelRequested);
                    break;
                case RemoteTaskStatus.CancelPending:
                    Update(JobStatus.CancelRequested);
                    break;
                case RemoteTaskStatus.Cancel:
                    Update(JobStatus.Cancel);
                    break;
                case RemoteTaskStatus.Error:
                    Update(JobStatus.Error);
                    break;
                case RemoteTaskStatus.Deleted:
                    Update(JobStatus.Deleted);
                    break;
            }
        }

        protected override void OnDispose() => _subscription.Dispose();
    }
}
