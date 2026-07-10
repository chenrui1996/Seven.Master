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
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmRaiseConsumer> _logger;

    /// <summary>构造函数</summary>
    public AlarmRaiseConsumer(IAlarmService alarmService, ILogger<AlarmRaiseConsumer> logger)
    {
        _alarmService = alarmService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<RaiseAlarmCommand> context)
    {
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
            _logger.LogWarning("MQ 抛警失败 Code={Code} Message={Message}", cmd.Code, result.Message);
    }
}
