using Domain.Dtos;
using Domain.Dtos.Pipelines;
using System;
using System.Collections.Generic;

namespace Application.Dojo.Pipelines
{
    public class Pipeline
    {
        private readonly IJob _rootJob;

        public Guid Id => Dto.Id;
        public PipelineDto Dto { get; }
        public Pipeline(PipelineParameters parameters, IJobFactory factory, IDojoDb db)
        {
            Dto = new PipelineDto
            {
                Id = Guid.NewGuid(),
                Name = parameters.Name,
                QueueName = parameters.QueueName,
            };

            _rootJob = factory.CreateJob(parameters.RootJob);
            Dto.RootJobId = _rootJob.Id;

            db.CreatePipeline(Dto);
        }

        public Pipeline(PipelineDto dto, IJobFactory factory, IDojoDb db)
        {
            Dto = dto;
            var jobDto = db.FetchJob(dto.RootJobId);
            if (jobDto != null)
                _rootJob = factory.CreateJob(jobDto);
            else
            {
                // Todo: Generate in error job
            }
        }

        public void Start() 
        {
            if (_rootJob.CanStart())
                _rootJob.Start();
        }

        public void Cancel() 
        {
            if (_rootJob.CanCancel())
                _rootJob.Cancel();
        }

        public void Delete() 
        {
            if (_rootJob.CanDelete())
                _rootJob.Delete();
        }
    }
}
