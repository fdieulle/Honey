using Domain.Dtos.Workflows;

namespace Application.Colony.Workflows
{
    public interface IJobFactory
    {
        IJob CreateJob(JobParameters parameters);
        IJob CreateJob(JobDto dto);
    }
}
