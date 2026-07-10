using Seven.Application.Messaging;
using Seven.Domain.Common;

namespace Seven.Application.Interfaces;

/// <summary>
/// 消息发布抽象（MassTransit / NoOp 实现可切换）
/// </summary>
public interface IMessagePublisher
{
    /// <summary>发布消息到消息队列</summary>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// 消息队列管理（状态查询、测试发布）
/// </summary>
public interface IMessageQueueService
{
    /// <summary>获取 MQ 连接状态</summary>
    Task<WebResponseContent> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>发布 RaiseAlarmCommand（WCS 入站测试）</summary>
    Task<WebResponseContent> PublishRaiseAlarmAsync(RaiseAlarmCommand command, CancellationToken cancellationToken = default);
}
