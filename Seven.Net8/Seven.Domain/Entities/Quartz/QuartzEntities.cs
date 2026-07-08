using Seven.Domain.Common;

namespace Seven.Domain.Entities.Quartz;

/// <summary>
/// 定时任务配置
/// </summary>
public class Sys_QuartzOptions : BaseEntity
{
    /// <summary>任务 Id</summary>
    public int Id { get; set; }

    /// <summary>任务名称</summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>任务分组</summary>
    public string? GroupName { get; set; }

    /// <summary>Cron 表达式</summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>请求 API 地址</summary>
    public string? ApiUrl { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;
}

/// <summary>
/// 定时任务执行日志
/// </summary>
public class Sys_QuartzLog : BaseEntity
{
    /// <summary>日志 Id</summary>
    public int LogId { get; set; }

    /// <summary>任务 Id</summary>
    public int TaskId { get; set; }

    /// <summary>执行结果</summary>
    public string? ResponseContent { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>耗时毫秒</summary>
    public int ElapsedMs { get; set; }
}
