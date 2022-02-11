using System;

namespace Ninja.Dto
{
    public enum JobState
    {
        Pending,
        Running,
        Done,
        Cancel,
        Error
    }

    public class Job
    {
        public string Id { get; set; }
        public string State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime ExpectedEndTime { get; set; }
        public double Progress { get; set; }
    }
}
