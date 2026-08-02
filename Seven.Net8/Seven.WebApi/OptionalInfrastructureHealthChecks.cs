using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using StackExchange.Redis;

namespace Seven.WebApi;

/// <summary>Redis：Provider=Redis 时 Ping，否则跳过</summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IOptions<CacheOptions> _options;

    public RedisHealthCheck(IOptions<CacheOptions> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!Enum.TryParse<CacheProvider>(opts.Provider, true, out var provider) || provider != CacheProvider.Redis)
            return HealthCheckResult.Healthy("redis skipped (not configured)");

        if (string.IsNullOrWhiteSpace(opts.RedisConnectionString))
            return HealthCheckResult.Degraded("redis enabled but connection string empty");

        try
        {
            using var mux = await ConnectionMultiplexer.ConnectAsync(opts.RedisConnectionString);
            var db = mux.GetDatabase();
            var pong = await db.PingAsync();
            return pong.TotalMilliseconds >= 0
                ? HealthCheckResult.Healthy($"redis ping {pong.TotalMilliseconds:F0}ms")
                : HealthCheckResult.Unhealthy("redis ping failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"redis unreachable: {ex.Message}");
        }
    }
}

/// <summary>RabbitMQ：Provider=RabbitMQ 时 TCP 探测 AMQP 端口</summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IOptions<MessageQueueOptions> _options;

    public RabbitMqHealthCheck(IOptions<MessageQueueOptions> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!Enum.TryParse<MessageQueueProvider>(opts.Provider, true, out var provider) || provider != MessageQueueProvider.RabbitMQ)
            return HealthCheckResult.Healthy("rabbitmq skipped (not configured)");

        var mq = opts.RabbitMq;
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(mq.Host, mq.Port, cancellationToken);
            return HealthCheckResult.Healthy($"rabbitmq tcp {mq.Host}:{mq.Port} ok");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"rabbitmq unreachable: {ex.Message}");
        }
    }
}

/// <summary>MinIO：Enabled 时 ListBuckets / EnsureBucket</summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly IOptions<MinioOptions> _options;

    public MinioHealthCheck(IOptions<MinioOptions> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
            return HealthCheckResult.Healthy("minio skipped (disabled)");

        try
        {
            var client = new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey)
                .WithSSL(opts.UseSsl)
                .Build();

            var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(opts.Bucket), cancellationToken);
            if (!exists)
            {
                await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(opts.Bucket), cancellationToken);
                return HealthCheckResult.Degraded($"minio ok, bucket `{opts.Bucket}` was created");
            }

            return HealthCheckResult.Healthy($"minio bucket `{opts.Bucket}` ok");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"minio unreachable: {ex.Message}");
        }
    }
}
