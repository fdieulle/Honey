using System;

namespace Domain.Dtos
{
    public class TaskStateDto
    {
        public Guid TaskId { get; set; }

        public double ProgressPercent { get; set; }

        public DateTime ExpectedEndTime { get; set; }

        public string Message { get; set; }
    }
}
