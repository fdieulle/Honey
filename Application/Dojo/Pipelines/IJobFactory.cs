using Domain.Dtos.Pipelines;

namespace Application.Dojo.Pipelines
{
    public interface IJobFactory
    {
        IJob CreateJob(JobParameters parameters);
        IJob CreateJob(JobDto dto);
    }
}
