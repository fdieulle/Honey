using System;

namespace Yumi
{
    public enum JobState
    {
        Pending,
        Running,
        Done,
        Cancel,
        Error,
        EndedWithoutSupervision,
    }

    public class Job
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime ExpectedEndTime { get; set; }
        public double Progress { get; set; }
    }
}
