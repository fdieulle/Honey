using Domain.Dtos.Workflows;

namespace Application.Dojo.Workflows
{
    public interface IJobFactory
    {
        IJob CreateJob(JobParameters parameters);
        IJob CreateJob(JobDto dto);
    }
}
