namespace Seven.Infrastructure.Messaging.Outbox;

/// <summary>与业务同一 DbContext 事务内写入 Outbox</summary>
public interface IOutboxStore
{
    /// <summary>入队（不 SaveChanges，由调用方提交事务）</summary>
    Task EnqueueAsync(string messageType, string payload, CancellationToken cancellationToken = default);
}

public class EfOutboxStore : IOutboxStore
{
    private readonly Seven.Infrastructure.Persistence.SevenDbContext _db;

    public EfOutboxStore(Seven.Infrastructure.Persistence.SevenDbContext db) => _db = db;

    public Task EnqueueAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            MessageType = messageType,
            Payload = payload,
            OccurredOn = DateTime.UtcNow,
        });
        return Task.CompletedTask;
    }
}
