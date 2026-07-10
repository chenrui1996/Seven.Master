using Seven.Application.Interfaces;
using Seven.Application.Messaging;

namespace Seven.Tests.Unit;

/// <summary>
/// NoOp 消息发布器单元测试
/// </summary>
public class NoOpMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_DoesNotThrow()
    {
        IMessagePublisher publisher = new Seven.Infrastructure.Messaging.NoOpMessagePublisher();
        await publisher.PublishAsync(new RaiseAlarmCommand { Code = "WCS001" });
    }
}
