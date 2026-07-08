namespace Seven.Application.Interfaces;

/// <summary>
/// 分布式/本地缓存服务，支持延迟双删策略。
/// </summary>
public interface ICacheService
{
    /// <summary>获取缓存</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>设置缓存</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>删除缓存</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>延迟双删：立即删除 + 延迟再次删除，防止缓存不一致</summary>
    Task RemoveWithDelayedDoubleDeleteAsync(string key, int delayMs = 500, CancellationToken cancellationToken = default);

    /// <summary>判断缓存是否存在</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// 密码哈希服务（BCrypt）
/// </summary>
public interface IPasswordHasher
{
    /// <summary>生成密码哈希</summary>
    string HashPassword(string password);

    /// <summary>验证密码</summary>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// JWT Token 服务
/// </summary>
public interface ITokenService
{
    /// <summary>生成 Access + Refresh Token</summary>
    (string AccessToken, string RefreshToken) GenerateTokens(int userId, string userName, int roleId);

    /// <summary>刷新 Access Token</summary>
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>吊销 Refresh Token</summary>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// 当前登录用户上下文
/// </summary>
public interface ICurrentUserService
{
    /// <summary>当前用户 Id</summary>
    int? UserId { get; }

    /// <summary>当前用户名</summary>
    string? UserName { get; }

    /// <summary>当前角色 Id</summary>
    int? RoleId { get; }
}

/// <summary>
/// 文件存储服务（MinIO / 本地）
/// </summary>
public interface IFileStorageService
{
    /// <summary>上传文件</summary>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>获取文件访问 URL</summary>
    Task<string> GetUrlAsync(string objectName, CancellationToken cancellationToken = default);
}
