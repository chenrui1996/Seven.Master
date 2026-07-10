using Seven.Application.Interfaces;

namespace Seven.Infrastructure.Messaging;

/// <summary>
/// 空实现：Provider=None 时不连接 RabbitMQ
/// </summary>
public class NoOpMessagePublisher : IMessagePublisher
{
    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
        Task.CompletedTask;
}
