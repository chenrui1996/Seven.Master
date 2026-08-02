namespace Seven.Domain.Enums;

/// <summary>
/// 数据库提供程序类型
/// </summary>
public enum DatabaseProvider
{
    /// <summary>MySQL</summary>
    MySql,

    /// <summary>SQL Server</summary>
    SqlServer,

    /// <summary>PostgreSQL</summary>
    PgSql
}

/// <summary>
/// 缓存提供程序类型
/// </summary>
public enum CacheProvider
{
    /// <summary>进程内内存缓存</summary>
    Memory,

    /// <summary>Redis 分布式缓存</summary>
    Redis
}

/// <summary>
/// 消息队列提供程序
/// </summary>
public enum MessageQueueProvider
{
    /// <summary>禁用 MQ（本地开发零依赖）</summary>
    None,

    /// <summary>RabbitMQ + MassTransit</summary>
    RabbitMQ
}

/// <summary>
/// 工作流步骤审批人类型
/// </summary>
public enum WorkFlowStepType
{
    /// <summary>按角色</summary>
    Role = 1,

    /// <summary>按用户</summary>
    User = 2,

    /// <summary>按部门（该部门下用户）</summary>
    Department = 3
}

/// <summary>
/// 数据权限范围
/// </summary>
public enum DataScope
{
    /// <summary>全部</summary>
    All = 0,

    /// <summary>本部门</summary>
    Department = 1,

    /// <summary>本部门及下级</summary>
    DepartmentAndChildren = 2,

    /// <summary>仅本人</summary>
    Self = 3
}

/// <summary>
/// 审批状态
/// </summary>
public enum AuditStatus
{
    /// <summary>待审核</summary>
    Pending = 0,

    /// <summary>审核通过</summary>
    Approved = 1,

    /// <summary>审核中</summary>
    InProgress = 2,

    /// <summary>审核未通过</summary>
    Rejected = 3,

    /// <summary>驳回</summary>
    Returned = 4
}

/// <summary>
/// 告警级别
/// </summary>
public enum AlarmLevel
{
    /// <summary>信息</summary>
    Info = 1,

    /// <summary>警告</summary>
    Warning = 2,

    /// <summary>错误</summary>
    Error = 3,

    /// <summary>严重</summary>
    Critical = 4
}

/// <summary>
/// 告警状态
/// </summary>
public enum AlarmStatus
{
    /// <summary>活跃，待处理</summary>
    Active = 0,

    /// <summary>已确认</summary>
    Acknowledged = 1,

    /// <summary>已清除</summary>
    Cleared = 2
}

/// <summary>
/// 设备运行状态
/// </summary>
public enum DeviceStatus
{
    /// <summary>离线</summary>
    [System.ComponentModel.Description("离线")]
    Offline = 0,

    /// <summary>在线</summary>
    [System.ComponentModel.Description("在线")]
    Online = 1,

    /// <summary>故障</summary>
    [System.ComponentModel.Description("故障")]
    Fault = 2,

    /// <summary>维护中</summary>
    [System.ComponentModel.Description("维护中")]
    Maintenance = 3
}
