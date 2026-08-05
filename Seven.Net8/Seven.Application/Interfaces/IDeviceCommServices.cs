using Seven.Domain.Common;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Enums;

namespace Seven.Application.Interfaces;

/// <summary>连接运行状态快照</summary>
public sealed class CommConnectionStatusDto
{
    public int CommConnectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CommProtocol Protocol { get; set; }
    public CommConnectionState State { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? LastConnectedAt { get; set; }
    public DateTimeOffset? LastDisconnectedAt { get; set; }
    public int ReconnectCount { get; set; }
    public int IoFailCount { get; set; }
}

/// <summary>点位读写值</summary>
public sealed class CommPointValueDto
{
    public int CommPointId { get; set; }
    public string Code { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? Error { get; set; }
}

/// <summary>原始读写请求</summary>
public sealed class CommRawIoRequest
{
    public int CommConnectionId { get; set; }
    public string Address { get; set; } = string.Empty;
    public CommDataType DataType { get; set; } = CommDataType.Int16;
    public object? Value { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>点位写请求</summary>
public sealed class CommWritePointRequest
{
    public int CommPointId { get; set; }
    public object? Value { get; set; }
}

/// <summary>规则触发推送</summary>
public sealed class CommRuleEventDto
{
    public int CommRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
    public string? Message { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>设备通讯运行时网关</summary>
public interface IDeviceCommGateway
{
    bool IsEnabled { get; }

    Task ConnectAsync(int connectionId, CancellationToken cancellationToken = default);
    Task DisconnectAsync(int connectionId, CancellationToken cancellationToken = default);
    Task ReconnectAsync(int connectionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommConnectionStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default);
    Task<CommConnectionStatusDto?> GetStatusAsync(int connectionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommPointValueDto>> ReadPointsAsync(IEnumerable<int> pointIds, CancellationToken cancellationToken = default);
    Task WritePointsAsync(IEnumerable<CommWritePointRequest> writes, CancellationToken cancellationToken = default);

    Task<object?> ReadRawAsync(CommRawIoRequest request, CancellationToken cancellationToken = default);
    Task WriteRawAsync(CommRawIoRequest request, CancellationToken cancellationToken = default);

    Task ReloadAsync(CancellationToken cancellationToken = default);
}

/// <summary>规则引擎</summary>
public interface ICommRuleEngine
{
    Task ReloadAsync(CancellationToken cancellationToken = default);
    Task ScanOnceAsync(CancellationToken cancellationToken = default);
    /// <summary>主动触发 WriteThenAwait / Sequence 等</summary>
    Task<CommRuleEventDto> TriggerAsync(int ruleId, CancellationToken cancellationToken = default);
}

/// <summary>通讯状态/事件推送</summary>
public interface IDeviceCommPushService
{
    Task PushConnectionStatusAsync(CommConnectionStatusDto status);
    Task PushRuleEventAsync(CommRuleEventDto evt);
}

/// <summary>连接配置 CRUD</summary>
public interface ICommConnectionService
{
    Task<PageGridData<CommConnection>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(CommConnection entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(CommConnection entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default);
}

/// <summary>点位 CRUD</summary>
public interface ICommPointService
{
    Task<PageGridData<CommPoint>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(CommPoint entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(CommPoint entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default);
}

/// <summary>规则 CRUD</summary>
public interface ICommRuleService
{
    Task<PageGridData<CommRule>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(CommRule entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(CommRule entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default);
    Task<PageGridData<CommEventLog>> GetEventLogsAsync(PageDataOptions options, CancellationToken cancellationToken = default);
}

/// <summary>规则定义根（JSON）</summary>
public sealed class CommRuleDefinition
{
    /// <summary>Monitor | MonitorThenRead | WriteThenAwait | Sequence</summary>
    public string Type { get; set; } = "Monitor";

    public List<CommRuleCondition> Monitor { get; set; } = [];
    public List<string> Read { get; set; } = [];
    public List<CommRuleWriteItem> Write { get; set; } = [];
    public List<CommRuleCondition> Await { get; set; } = [];
    public int? AwaitTimeoutMs { get; set; }
    public string ConditionMode { get; set; } = "all";
    public CommRuleEmit? Emit { get; set; }
    public List<CommRuleStep>? Steps { get; set; }
}

public sealed class CommRuleCondition
{
    public string Point { get; set; } = string.Empty;
    /// <summary>eq/ne/gt/gte/lt/lte/changed/rising/falling</summary>
    public string Op { get; set; } = "eq";
    public object? Value { get; set; }
}

public sealed class CommRuleWriteItem
{
    public string Point { get; set; } = string.Empty;
    public object? Value { get; set; }
}

public sealed class CommRuleEmit
{
    public string EventName { get; set; } = string.Empty;
    public bool RaiseAlarm { get; set; }
    public string? AlarmCode { get; set; }
    public bool WriteHotStore { get; set; }
    public string? HotStoreKeyPrefix { get; set; }
}

public sealed class CommRuleStep
{
    /// <summary>Read | Write | Delay | Condition | Emit</summary>
    public string Action { get; set; } = "Read";
    public List<string>? Points { get; set; }
    public List<CommRuleWriteItem>? Writes { get; set; }
    public List<CommRuleCondition>? Conditions { get; set; }
    public string? ConditionMode { get; set; }
    public int? DelayMs { get; set; }
    public CommRuleEmit? Emit { get; set; }
}
