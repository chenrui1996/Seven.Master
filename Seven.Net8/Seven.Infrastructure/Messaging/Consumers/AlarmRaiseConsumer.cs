using MassTransit;
using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;
using Seven.Application.Messaging;

namespace Seven.Infrastructure.Messaging.Consumers;

/// <summary>
/// 消费 RaiseAlarmCommand → 调用告警服务（WCS/边缘系统 MQ 入站）
/// </summary>
public class AlarmRaiseConsumer : IConsumer<RaiseAlarmCommand>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

    private readonly IAlarmService _alarmService;
    private readonly ICacheService _cache;
    private readonly ILogger<AlarmRaiseConsumer> _logger;

    /// <summary>构造函数</summary>
    public AlarmRaiseConsumer(
        IAlarmService alarmService,
        ICacheService cache,
        ILogger<AlarmRaiseConsumer> logger)
    {
        _alarmService = alarmService;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<RaiseAlarmCommand> context)
    {
        var idempotencyKey = ResolveIdempotencyKey(context);
        var cacheKey = $"mq:idempotency:alarm-raise:{idempotencyKey}";

        if (await _cache.ExistsAsync(cacheKey, context.CancellationToken))
        {
            _logger.LogInformation("MQ 抛警重复消息已跳过 Key={Key}", idempotencyKey);
            return;
        }

        var cmd = context.Message;
        _logger.LogInformation("MQ 收到抛警命令 Code={Code} Source={Source}", cmd.Code, cmd.Source);

        var result = await _alarmService.RaiseAsync(new RaiseAlarmRequest
        {
            Code = cmd.Code,
            Source = cmd.Source ?? "MessageQueue",
            DeviceName = cmd.DeviceName,
            Params = cmd.Params,
            ExtraData = cmd.ExtraData
        }, context.CancellationToken);

        if (!result.Status)
        {
            _logger.LogWarning("MQ 抛警失败 Code={Code} Message={Message}", cmd.Code, result.Message);
            return;
        }

        await _cache.SetAsync(cacheKey, "1", IdempotencyTtl, context.CancellationToken);
    }

    static string ResolveIdempotencyKey(ConsumeContext context)
    {
        if (context.Headers.TryGetHeader("Idempotency-Key", out var header) && header != null)
        {
            var text = header.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        if (context.MessageId is { } messageId)
            return messageId.ToString();

        return context.CorrelationId?.ToString() ?? Guid.NewGuid().ToString("N");
    }
}

/// <summary>入站 Consumer 重试（失败后 MassTransit 默认进入 RabbitMQ _error 死信队列）</summary>
public class AlarmRaiseConsumerDefinition : ConsumerDefinition<AlarmRaiseConsumer>
{
    public AlarmRaiseConsumerDefinition()
    {
        EndpointName = "alarm-raise";
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AlarmRaiseConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
