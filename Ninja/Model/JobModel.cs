using Ninja.Dto;
using System;

namespace Ninja.Model
{
    public class JobModel
    {
        public Guid Id { get; set; }

        public string Command { get; set; }

        public string Arguments { get; set; }

        public int Pid { get; set; }

        public DateTime StartTime { get; set; }

        public JobState State { get; set; }

        public DateTime EndTime { get; set; }

        public string WorkingFolder { get; set; }
    }
}
