using System.Data;

namespace ASPA0011_1.Models
{
    public class ChannelInfo
    {
        public Guid Guid { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public ChannelStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChannelCreateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public ChannelStatus? Status { get; set; }
    }

    public class ChannelStateChangeRequest
    {
        public ChannelStatus Status { get; set; }
    }

    public class ChannelDeleteRequest
    {
        public ChannelStatus? status { get; set; }
    }

    public class QueueOperationRequest
    {
        public Guid ChannelGuid { get; set; }
        public QueueOperationType Operation { get; set; }
        public object? Data { get; set; }
        public int? TimeoutSeconds { get; set; }
    }

    public class QueueOperationResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? Error { get; set; }
    }
    public class ChannelUpdateRequest
    {
        public Guid? Guid { get; set; }
        public ChannelStatus Status { get; set; }
    }

    public enum ChannelStatus
    {
        Active,
        Closed,
        Paused
    }

    public enum QueueOperationType
    {
        Enqueue,
        Dequeue,
        Peek
    }
}