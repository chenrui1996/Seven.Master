using MassTransit;
using Seven.Application.Interfaces;

namespace Seven.Infrastructure.Messaging;

/// <summary>
/// MassTransit 发布适配
/// </summary>
public class MassTransitMessagePublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>构造函数</summary>
    public MassTransitMessagePublisher(IPublishEndpoint publishEndpoint) =>
        _publishEndpoint = publishEndpoint;

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
        _publishEndpoint.Publish(message, cancellationToken);
}
