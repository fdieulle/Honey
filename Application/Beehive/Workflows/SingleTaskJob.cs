using Domain;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;

namespace Application.Beehive.Workflows
{
    public class SingleTaskJob : Job<SingleTaskJobParameters, SingleTaskJobDto>
    {
        private readonly Colony _colony;
        private readonly ITaskTracker _tracker;
        private IDisposable _subscription;

        public SingleTaskJob(SingleTaskJobParameters parameters, Colony colony, ITaskTracker tracker, IBeehiveDb db)
            : base(parameters, db)
        {
            Dto.Parameters = parameters.Task ?? new TaskParameters();
            _colony = colony;
            _tracker = tracker;

            db.CreateJob(Dto);
        }

        public SingleTaskJob(SingleTaskJobDto dto, Colony colony, ITaskTracker tracker, IBeehiveDb db)
            : base(dto, db) 
        {
            _colony = colony;
            _tracker = tracker;

            if (dto.TaskId != Guid.Empty)
                _subscription = tracker.Subscribe(dto.TaskId, OnTaskUpdated);
        }

        public override void Start()
        {
            if (!Status.CanStart()) return;

            StartTask();
        }

        public override void Cancel()
        {
            if (!Status.CanCancel()) return;

            if (Status == JobStatus.Pending)
            {
                Update(JobStatus.Cancel);
                return;
            }

            Update(JobStatus.CancelRequested);
            _colony.CancelTask(Dto.TaskId);
        }

        public override void Recover()
        {
            if (!Status.CanRecover()) return;

            _subscription?.Dispose();
            _colony.DeleteTask(Dto.TaskId);

            StartTask();
        }

        public override void Delete()
        {
            if (!Status.CanDelete()) return;

            Update(JobStatus.DeleteRequested);

            _colony.DeleteTask(Dto.TaskId);
        }

        private void StartTask()
        {
            if (_colony == null)
            {
                Update(JobStatus.Error); // TODO: Mention that the colony doesn't exist.
                return;
            }

            Update(JobStatus.Running);

            Dto.TaskId = _colony.StartTask(Dto.Name, Dto.Parameters);
            if (Dto.TaskId == Guid.Empty)
            {
                Update(JobStatus.Error); // TODO: Mention that the task failed to start.
                return;
            }

            _subscription = _tracker.Subscribe(Dto.TaskId, OnTaskUpdated);
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
