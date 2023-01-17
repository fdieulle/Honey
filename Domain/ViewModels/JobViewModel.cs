using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;

namespace Domain.ViewModels
{
    public class JobViewModel
    {
        public static JobViewModel Empty { get; } = new JobViewModel { Name = "Empty", Type = "Unknown" };

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Host { get; set; }
        public JobStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public double Progress { get; set; }
        public List<JobViewModel> Children { get; set; } = new List<JobViewModel>();
    }
}
