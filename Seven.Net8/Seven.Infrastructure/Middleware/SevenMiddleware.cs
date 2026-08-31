using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Seven.Domain.Common;

namespace Seven.Infrastructure.Middleware;

/// <summary>统一异常处理：AppException 按业务码返回；未知异常 → SYS.UNHANDLED。</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "业务异常 {Code}: {Message}", ex.Code, ex.Message);
            await WriteErrorAsync(context, ex.HttpStatus, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                ExceptionCodes.Sys.Unhandled, ex.Message);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var traceId = context.Items["TraceId"]?.ToString() ?? context.TraceIdentifier;
        var body = new { status = false, code, message, traceId, data = (object?)null };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

/// <summary>
/// TraceId 中间件，便于日志追踪
/// </summary>
public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>构造函数</summary>
    public TraceIdMiddleware(RequestDelegate next) => _next = next;

    /// <summary>处理请求</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Trace-Id"] = traceId;
        context.Items["TraceId"] = traceId;
        await _next(context);
    }
}

/// <summary>
/// 请求日志中间件
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>构造函数</summary>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>处理请求并记录耗时</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();
        _logger.LogInformation("{Method} {Path} => {StatusCode} ({Elapsed}ms)",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
