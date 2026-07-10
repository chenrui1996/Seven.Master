namespace Seven.Infrastructure.Configuration;

/// <summary>
/// 告警模块配置（appsettings.json → Alarm 节点）
/// </summary>
public class AlarmOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "Alarm";

    /// <summary>启动时同步到数据库的默认报警码（数据库已有同 Code 则跳过）</summary>
    public List<AlarmCodeDefinition> Codes { get; set; } = [];
}

/// <summary>
/// appsettings 中的报警码定义
/// </summary>
public class AlarmCodeDefinition
{
    /// <summary>报警码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>报警信息模板</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>级别 1-4</summary>
    public int Level { get; set; } = 2;

    /// <summary>分类</summary>
    public string? Category { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
