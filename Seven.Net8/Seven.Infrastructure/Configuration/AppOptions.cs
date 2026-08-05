namespace Seven.Infrastructure.Configuration;

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "Database";

    /// <summary>数据库类型：MySql / SqlServer / PgSql</summary>
    public string Provider { get; set; } = "MySql";

    /// <summary>连接字符串</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 启动时是否自动执行 EF 迁移（MigrateAsync）。
    /// 生产环境应保持 false，由发布流水线显式迁移。
    /// </summary>
    public bool MigrateOnStartup { get; set; }
}

/// <summary>
/// 缓存配置
/// </summary>
public class CacheOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "Cache";

    /// <summary>缓存类型：Memory / Redis</summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>Redis 连接字符串</summary>
    public string RedisConnectionString { get; set; } = "127.0.0.1:6379";

    /// <summary>延迟双删间隔毫秒</summary>
    public int DelayedDeleteMs { get; set; } = 500;
}

/// <summary>
/// JWT 配置
/// </summary>
public class JwtOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "Jwt";

    /// <summary>签名密钥</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>签发者</summary>
    public string Issuer { get; set; } = "Seven";

    /// <summary>受众</summary>
    public string Audience { get; set; } = "Seven";

    /// <summary>Access Token 有效期（分钟）</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh Token 有效期（天）</summary>
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>
/// MinIO 对象存储配置
/// </summary>
public class MinioOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "MinIO";

    /// <summary>是否启用 MinIO（false 时使用本地存储）</summary>
    public bool Enabled { get; set; }

    /// <summary>服务端点</summary>
    public string Endpoint { get; set; } = "127.0.0.1:9000";

    /// <summary>Access Key</summary>
    public string AccessKey { get; set; } = "minioadmin";

    /// <summary>Secret Key</summary>
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>Bucket 名称</summary>
    public string Bucket { get; set; } = "seven";

    /// <summary>是否启用 SSL</summary>
    public bool UseSsl { get; set; }
}

/// <summary>邮件 SMTP</summary>
public class MailOptions
{
    public const string SectionName = "Mail";
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool UseSsl { get; set; }
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "noreply@seven.local";
    public string FromName { get; set; } = "Seven Master";
}

/// <summary>多租户</summary>
public class TenantOptions
{
    public const string SectionName = "Tenant";
    /// <summary>是否启用共享库 TenantId 过滤</summary>
    public bool Enabled { get; set; }
}

/// <summary>安全相关</summary>
public class SecurityOptions
{
    public const string SectionName = "Security";
    public bool CaptchaEnabled { get; set; } = true;
    public int LoginPermitLimit { get; set; } = 20;
    public int IdempotencySeconds { get; set; } = 5;
    public string? IpWhitelist { get; set; }
}

/// <summary>
/// CORS 配置
/// </summary>
public class CorsOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "Cors";

    /// <summary>允许的前端地址，逗号分隔</summary>
    public string Origins { get; set; } = "http://localhost:5173";
}

/// <summary>
/// 热数据通道配置（路径占用、流量、车辆实时状态等）。
/// 与 Features.HotStore 为 AND 关系。
/// </summary>
public class HotStoreOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "HotStore";

    /// <summary>存储类型：Memory / Redis</summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>Redis 连接字符串；为空时回退 Cache:RedisConnectionString</summary>
    public string RedisConnectionString { get; set; } = "";

    /// <summary>热 key 前缀，与 menu:/dict: 隔离</summary>
    public string KeyPrefix { get; set; } = "hot:";

    /// <summary>启动时执行已注册的 IHotStoreWarmup</summary>
    public bool WarmupOnStartup { get; set; } = true;

    /// <summary>预热超时秒数</summary>
    public int WarmupTimeoutSeconds { get; set; } = 30;

    /// <summary>是否启用异步落库</summary>
    public bool PersistEnabled { get; set; } = true;

    /// <summary>落库节拍（毫秒）</summary>
    public int PersistIntervalMs { get; set; } = 1000;

    /// <summary>每批最大变更条数</summary>
    public int PersistBatchSize { get; set; } = 200;

    /// <summary>热 key 默认过期秒数；0=不过期</summary>
    public int DefaultTtlSeconds { get; set; }

    /// <summary>落库实体短名/表名列表，跳过字段审计</summary>
    public List<string> AuditExcludeEntities { get; set; } = [];

    /// <summary>单活写入假设；多实例需 Redis + 业务选主</summary>
    public bool SingleWriter { get; set; } = true;

    /// <summary>注册四向车 Demo 预热/落库/节拍（仅演示，生产保持 false）</summary>
    public bool EnableDemoScheduler { get; set; }
}

/// <summary>
/// 设备通讯配置。与 Features.DeviceComm 为 AND 关系。
/// </summary>
public class DeviceCommOptions
{
    public const string SectionName = "DeviceComm";

    /// <summary>单活写入假设；多实例需业务选主</summary>
    public bool SingleWriter { get; set; } = true;

    public int DefaultConnectTimeoutMs { get; set; } = 3000;
    public int DefaultIoTimeoutMs { get; set; } = 2000;

    public DeviceCommRetryOptions Retry { get; set; } = new();
    public DeviceCommReconnectOptions Reconnect { get; set; } = new();

    public int RuleScanIntervalMs { get; set; } = 200;
    public bool EnableAlarmOnDisconnect { get; set; } = true;
    public string AlarmCodeDisconnect { get; set; } = "DEV001";
    public bool WriteHotStoreOnPointChange { get; set; }
}

/// <summary>IO 失败重试</summary>
public class DeviceCommRetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int BackoffMs { get; set; } = 500;
}

/// <summary>断连自动重连</summary>
public class DeviceCommReconnectOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalMs { get; set; } = 5000;
    public int MaxIntervalMs { get; set; } = 60000;
}

/// <summary>
/// 功能总开关。关闭后后端不注册对应宿主/中间件，前端隐藏菜单与入口。
/// 与细节节（MinIO/Mail/Tenant/Security/MessageQueue）为 AND 关系。
/// </summary>
public class FeatureOptions
{
    public const string SectionName = "Features";

    public bool WorkFlow { get; set; }
    public bool Quartz { get; set; }
    public bool SignalR { get; set; } = true;
    public bool Alarm { get; set; } = true;
    public bool MessageQueue { get; set; }
    public bool Outbox { get; set; }
    public bool Mail { get; set; }
    public bool MinIO { get; set; }
    public bool Tenant { get; set; }
    public bool Captcha { get; set; }
    public bool RateLimit { get; set; } = true;
    public bool Idempotency { get; set; } = true;
    public bool DataScope { get; set; } = true;
    public bool AuditInterceptor { get; set; } = true;
    public bool Builder { get; set; } = true;

    /// <summary>热数据通道（路径/流量等高频读写）；细节见 HotStore 节</summary>
    public bool HotStore { get; set; }

    /// <summary>设备通讯（Step7 / Modbus TCP）；细节见 DeviceComm 节</summary>
    public bool DeviceComm { get; set; }

    /// <summary>按属性名读取开关（忽略大小写）</summary>
    public bool IsEnabled(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName)) return true;
        var prop = typeof(FeatureOptions).GetProperty(
            featureName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (prop?.PropertyType != typeof(bool)) return true;
        return (bool)(prop.GetValue(this) ?? true);
    }
}
