using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Middleware;

/// <summary>写操作防重复提交：Header X-Idempotency-Key</summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ICacheService cache,
        IOptions<SecurityOptions> options,
        IOptions<FeatureOptions> features)
    {
        if (!features.Value.Idempotency)
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method))
        {
            var key = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(key))
            {
                var cacheKey = $"idem:{key}";
                if (await cache.ExistsAsync(cacheKey, context.RequestAborted))
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":false,\"message\":\"重复提交，请稍后重试\"}");
                    return;
                }
                var seconds = Math.Max(1, options.Value.IdempotencySeconds);
                await cache.SetAsync(cacheKey, "1", TimeSpan.FromSeconds(seconds), context.RequestAborted);
            }
        }

        await _next(context);
    }
}

/// <summary>可选 IP 白名单</summary>
public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _whitelist;

    public IpWhitelistMiddleware(RequestDelegate next, IOptions<SecurityOptions> options)
    {
        _next = next;
        _whitelist = (options.Value.IpWhitelist ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_whitelist.Count > 0)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";
            if (!_whitelist.Contains(ip) && !_whitelist.Contains("*"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("IP not allowed");
                return;
            }
        }
        await _next(context);
    }
}
