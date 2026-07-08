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
