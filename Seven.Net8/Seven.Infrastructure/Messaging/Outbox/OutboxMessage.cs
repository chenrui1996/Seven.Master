using Seven.Domain.Common;

namespace Seven.Infrastructure.Messaging.Outbox;

/// <summary>出站消息 Outbox（与业务事务同库）</summary>
public class OutboxMessage : BaseEntity
{
    public long Id { get; set; }
    public string MessageType { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
