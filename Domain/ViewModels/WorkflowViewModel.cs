using Domain.Dtos.Workflows;
using System;

namespace Domain.ViewModels
{
    public class WorkflowViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Queue { get; set; }

        public JobStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public double Progress { get; set; }

        public bool CanCancel { get; set; }
        public bool CanRecover { get; set; }
        public bool CanDelete { get; set; }
    }
}
