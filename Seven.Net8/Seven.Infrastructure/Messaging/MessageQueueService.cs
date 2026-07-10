using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Application.Messaging;
using Seven.Domain.Common;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Messaging;

/// <summary>
/// 消息队列状态与测试发布
/// </summary>
public class MessageQueueService : IMessageQueueService
{
    private readonly IMessagePublisher _publisher;
    private readonly MessageQueueOptions _options;

    /// <summary>构造函数</summary>
    public MessageQueueService(IMessagePublisher publisher, IOptions<MessageQueueOptions> options)
    {
        _publisher = publisher;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<WebResponseContent> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var provider = Enum.TryParse<MessageQueueProvider>(_options.Provider, true, out var p)
            ? p
            : MessageQueueProvider.None;

        return Task.FromResult(WebResponseContent.Ok(data: new
        {
            provider = provider.ToString(),
            consumersEnabled = _options.EnableConsumers,
            rabbitMq = provider == MessageQueueProvider.RabbitMQ
                ? new { _options.RabbitMq.Host, _options.RabbitMq.Port, _options.RabbitMq.VirtualHost }
                : null
        }));
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> PublishRaiseAlarmAsync(RaiseAlarmCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            return WebResponseContent.Error("报警码不能为空");

        await _publisher.PublishAsync(command, cancellationToken);
        return WebResponseContent.Ok("消息已发布到队列", data: command);
    }
}
