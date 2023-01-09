using Domain.Dtos.Pipelines;

namespace Application.Dojo.Pipelines
{
    public class LinkedJobs : Job<LinkedJobsParameters, LinkedJobsDto>
    {
        private readonly IJob _jobA;
        private readonly IJob _jobB;

        public LinkedJobs(LinkedJobsParameters parameters, IJobFactory factory, IDojoDb db) 
            : base(parameters, db)
        {
            Dto.LinkType = parameters.LinkType;
            _jobA = factory.CreateJob(parameters.JobA);
            Dto.JobAId = _jobA.Id;
            _jobB = factory.CreateJob(parameters.JobB);
            Dto.JobBId = _jobB.Id;

            db.CreateJob(Dto);

            // TODO: Should we subscribe during the Start phase to avoid useless updates from previous job dependencies
            _jobA.Updated += OnJobAUpdated;
            _jobB.Updated += OnJobBUpdated;
        }

        public LinkedJobs(LinkedJobsDto dto, IJobFactory factory, IDojoDb db) 
            : base(dto, db)
        {
            _jobA = factory.CreateJob(db.FetchJob(dto.JobAId));
            _jobB = factory.CreateJob(db.FetchJob(dto.JobBId));

            // Todo: Handle errors

            _jobA.Updated += OnJobAUpdated;
            _jobB.Updated += OnJobBUpdated;

            // Todo: Handle dojo down time and status changes during the down time
        }

        public override void Start()
        {
            if (!this.CanStart() && !_jobA.CanStart() && !_jobB.CanStart())
                return;
            
            switch (Dto.LinkType)
            {
                case LinkedJobType.FinishToStart:
                    _jobA.Start();
                    break;
                case LinkedJobType.StartToStart:
                    _jobA.Start();
                    break;
                case LinkedJobType.FinishToFinish:
                    _jobA.Start();
                    _jobB.Start();
                    break;
                case LinkedJobType.StartToFinish:
                    _jobB.Start();
                    break;
            }
        }
        public override void Cancel()
        {
            if (!this.CanCancel())
                return;

            Update(JobStatus.CancelRequested);

            if (_jobA.CanCancel())
                _jobA.Cancel();
            if (_jobB.CanCancel())
                _jobB.Cancel();
        }

        public override void Delete()
        {
            if (!this.CanDelete() || !_jobA.CanDelete() || !_jobB.CanDelete())
                return;

            _jobA.Delete();
            _jobB.Delete();
        }
        
        private void OnJobAUpdated(IJob jobA)
        {
            switch (Dto.LinkType)
            {
                case LinkedJobType.FinishToStart:
                    if (jobA.Status == JobStatus.Completed)
                        _jobB.Start();
                    break;
                case LinkedJobType.StartToStart:
                    if (jobA.Status == JobStatus.Running)
                        _jobB.Start();
                    break;
            }

            Update();
        }

        private void OnJobBUpdated(IJob jobB)
        {
            switch (Dto.LinkType)
            {
                case LinkedJobType.StartToFinish:
                    if (jobB.Status == JobStatus.Completed)
                        _jobA.Start();
                    break;
            }

            Update();
        }

        private void Update()
        {
            if (_jobA.Status == JobStatus.Error || _jobB.Status == JobStatus.Error)
                Update(JobStatus.Error);
            else if (_jobA.Status == JobStatus.Cancel || _jobB.Status == JobStatus.Cancel)
                Update(JobStatus.Cancel);
            else if (_jobA.Status == JobStatus.Deleted && _jobB.Status == JobStatus.Deleted)
                Update(JobStatus.Deleted);
            else if (Status == JobStatus.CancelRequested)
                Update(JobStatus.CancelRequested);
            else if (_jobA.Status == JobStatus.Completed && _jobB.Status == JobStatus.Completed)
                Update(JobStatus.Completed);
            else if (_jobA.Status != JobStatus.Pending || _jobB.Status != JobStatus.Pending)
                Update(JobStatus.Running);
            else
                Update(JobStatus.Pending);
        }
        protected override void OnDispose()
        {
            _jobA.Updated -= OnJobAUpdated;
            _jobB.Updated -= OnJobBUpdated;
        }
    }
}
