using Domain.Dtos.Workflows;

namespace Domain.Entities.Workflows
{
    public class ManyJobsEntity : JobEntity
    {
        public JobsBehavior Behavior { get; set; }
        public string JobIds { get; set; }
    }
}
