using System;

namespace Yumi
{
    public enum MessageType
    {
        Info,
        Error,
        Exit
    }

    public class JobMessage
    {
        public Guid JobId { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }

        public JobMessage() { }
        public JobMessage(Guid jobId, DateTime timestamp, MessageType type, string message)
        {
            JobId = jobId;
            Timestamp = timestamp;
            Type = type;
            Message = message;
        }
    }
}
