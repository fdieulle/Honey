using System;

namespace Domain.Dtos
{
    public enum MessageType
    {
        Info,
        Error,
        Exit
    }

    public class TaskMessageDto
    {
        public Guid JobId { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }

        public TaskMessageDto() { }
        public TaskMessageDto(Guid jobId, DateTime timestamp, MessageType type, string message)
        {
            JobId = jobId;
            Timestamp = timestamp;
            Type = type;
            Message = message;
        }
    }
}
