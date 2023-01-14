using Domain.Dtos.Pipelines;

namespace Domain.Entities.Pipelines
{
    public class ManyJobsEntity : JobEntity
    {
        public JobsBehavior Behavior { get; set; }
        public string JobIds { get; set; }
    }
}
