using Seven.Domain.Common;

namespace Seven.Domain.Entities.Alarm;

/// <summary>
/// 告警码配置（报警码 → 默认消息、级别、分类）
/// </summary>
public class Sys_AlarmCode : BaseEntity
{
    /// <summary>主键</summary>
    public int AlarmCode_Id { get; set; }

    /// <summary>报警码，全局唯一，如 WCS001</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>报警信息模板，支持占位符 {DeviceName}、{Source} 等</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>级别：1 信息 2 警告 3 错误 4 严重</summary>
    public int Level { get; set; } = 2;

    /// <summary>分类：WMS / WCS / Device / System</summary>
    public string? Category { get; set; }

    /// <summary>是否启用 1/0</summary>
    public int Enable { get; set; } = 1;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 告警记录（后端抛出后持久化，并 SignalR 推送到前端）
/// </summary>
public class Sys_Alarm : BaseEntity
{
    /// <summary>主键</summary>
    public int Alarm_Id { get; set; }

    /// <summary>报警码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>解析后的报警信息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>级别：1 信息 2 警告 3 错误 4 严重</summary>
    public int Level { get; set; }

    /// <summary>分类</summary>
    public string? Category { get; set; }

    /// <summary>来源（服务名、模块名等）</summary>
    public string? Source { get; set; }

    /// <summary>关联设备</summary>
    public string? DeviceName { get; set; }

    /// <summary>状态：0 活跃 1 已确认 2 已清除</summary>
    public int Status { get; set; }

    /// <summary>确认人</summary>
    public string? AckUserName { get; set; }

    /// <summary>确认时间</summary>
    public DateTime? AckDate { get; set; }

    /// <summary>扩展 JSON</summary>
    public string? ExtraData { get; set; }
}
