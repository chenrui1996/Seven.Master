namespace Seven.Application.Messaging;

/// <summary>
/// 入站：WCS/边缘系统通过 MQ 请求抛出告警（Consumer → IAlarmService.RaiseAsync）
/// </summary>
public record RaiseAlarmCommand
{
    /// <summary>报警码</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>来源</summary>
    public string? Source { get; init; }

    /// <summary>设备名</summary>
    public string? DeviceName { get; init; }

    /// <summary>模板占位符</summary>
    public Dictionary<string, string>? Params { get; init; }

    /// <summary>扩展 JSON</summary>
    public string? ExtraData { get; init; }
}

/// <summary>
/// 出站：告警已产生，供 MES/监控等外部系统订阅
/// </summary>
public record AlarmRaisedEvent
{
    /// <summary>告警 Id</summary>
    public int AlarmId { get; init; }

    /// <summary>报警码</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>报警信息</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>级别</summary>
    public int Level { get; init; }

    /// <summary>分类</summary>
    public string? Category { get; init; }

    /// <summary>来源</summary>
    public string? Source { get; init; }

    /// <summary>设备</summary>
    public string? DeviceName { get; init; }

    /// <summary>发生时间</summary>
    public DateTime OccurredAt { get; init; }
}
