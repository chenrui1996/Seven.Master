using System.Security.Cryptography;
using System.Text;
using Seven.Application.Interfaces;
using Seven.Domain.Common;

namespace Seven.Infrastructure.Security;

public interface ICaptchaService
{
    Task<WebResponseContent> CreateAsync(CancellationToken ct = default);
    Task<bool> ValidateAsync(string? key, string? code, CancellationToken ct = default);
}

public class CaptchaService : ICaptchaService
{
    private readonly ICacheService _cache;

    public CaptchaService(ICacheService cache) => _cache = cache;

    public async Task<WebResponseContent> CreateAsync(CancellationToken ct = default)
    {
        var code = RandomNumberGenerator.GetInt32(1000, 9999).ToString();
        var key = Guid.NewGuid().ToString("N");
        await _cache.SetAsync($"captcha:{key}", code, TimeSpan.FromMinutes(5), ct);
        // 简化：返回明文验证码（生产可改为图片 base64）
        return WebResponseContent.Ok(data: new { key, code, expiresIn = 300 });
    }

    public async Task<bool> ValidateAsync(string? key, string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(code)) return false;
        var cached = await _cache.GetAsync<string>($"captcha:{key}", ct);
        await _cache.RemoveAsync($"captcha:{key}", ct);
        return string.Equals(cached, code.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
