using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;
using Seven.Application.Messaging;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Messaging.Outbox;

/// <summary>轮询 Outbox 表并发布（幂等：ProcessedOn 标记）</summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
                var publisher = scope.ServiceProvider.GetService<IMessagePublisher>();
                if (publisher == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                var batch = await db.OutboxMessages
                    .Where(m => m.ProcessedOn == null && !m.IsDeleted)
                    .OrderBy(m => m.Id)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in batch)
                {
                    try
                    {
                        if (string.Equals(msg.MessageType, nameof(AlarmRaisedEvent), StringComparison.Ordinal))
                        {
                            var evt = JsonSerializer.Deserialize<AlarmRaisedEvent>(msg.Payload);
                            if (evt != null)
                                await publisher.PublishAsync(evt, stoppingToken);
                            else
                                throw new InvalidOperationException("AlarmRaisedEvent payload invalid");
                        }
                        else
                        {
                            // 通用出站：以字符串载荷发布（具体消费者按 MessageType 处理）
                            await publisher.PublishAsync(new OutboxDispatchEvent(msg.MessageType, msg.Payload), stoppingToken);
                        }

                        msg.ProcessedOn = DateTime.UtcNow;
                        msg.Error = null;
                    }
                    catch (Exception ex)
                    {
                        msg.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                        _logger.LogWarning(ex, "Outbox 消息 {Id} 发布失败", msg.Id);
                    }
                }

                if (batch.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Outbox 轮询异常");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public record OutboxDispatchEvent(string MessageType, string Payload);
