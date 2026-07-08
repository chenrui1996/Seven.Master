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
