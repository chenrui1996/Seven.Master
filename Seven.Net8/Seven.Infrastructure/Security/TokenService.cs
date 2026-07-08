using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Security;

/// <summary>
/// JWT + Refresh Token 服务实现
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly ICacheService _cache;

    /// <summary>构造函数</summary>
    public TokenService(IOptions<JwtOptions> options, ICacheService cache)
    {
        _options = options.Value;
        _cache = cache;
    }

    /// <inheritdoc />
    public (string AccessToken, string RefreshToken) GenerateTokens(int userId, string userName, int roleId)
    {
        var accessToken = CreateAccessToken(userId, userName, roleId);
        var refreshToken = Guid.NewGuid().ToString("N");
        var refreshKey = GetRefreshKey(refreshToken);
        _cache.SetAsync(refreshKey, new RefreshTokenPayload(userId, userName, roleId),
            TimeSpan.FromDays(_options.RefreshTokenDays)).GetAwaiter().GetResult();
        return (accessToken, refreshToken);
    }

    /// <inheritdoc />
    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var payload = await _cache.GetAsync<RefreshTokenPayload>(GetRefreshKey(refreshToken), cancellationToken);
        if (payload == null) return null;
        await _cache.RemoveAsync(GetRefreshKey(refreshToken), cancellationToken);
        return GenerateTokens(payload.UserId, payload.UserName, payload.RoleId);
    }

    /// <inheritdoc />
    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(GetRefreshKey(refreshToken), cancellationToken);

    private string CreateAccessToken(int userId, string userName, int roleId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim("RoleId", roleId.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetRefreshKey(string refreshToken) => $"refresh:{refreshToken}";

    /// <summary>Refresh Token 缓存载荷</summary>
    private sealed record RefreshTokenPayload(int UserId, string UserName, int RoleId);
}

/// <summary>
/// 从 HttpContext 读取当前登录用户
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>构造函数</summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public int? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? userId : null;
        }
    }

    /// <inheritdoc />
    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc />
    public int? RoleId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue("RoleId");
            return int.TryParse(id, out var roleId) ? roleId : null;
        }
    }
}
