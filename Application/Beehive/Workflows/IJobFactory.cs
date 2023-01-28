using Domain.Dtos.Workflows;

namespace Application.Beehive.Workflows
{
    public interface IJobFactory
    {
        IJob CreateJob(JobParameters parameters);
        IJob CreateJob(JobDto dto);
    }
}
