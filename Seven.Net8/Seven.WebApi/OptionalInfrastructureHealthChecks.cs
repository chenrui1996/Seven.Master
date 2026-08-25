using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Seven.Infrastructure.Configuration;

namespace Seven.WebApi;

/// <summary>MinIO：Enabled 时只读探测 BucketExists，不建桶</summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly IOptions<MinioOptions> _options;

    public MinioHealthCheck(IOptions<MinioOptions> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        try
        {
            var client = new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey)
                .WithSSL(opts.UseSsl)
                .Build();

            var exists = await client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(opts.Bucket),
                cancellationToken);

            if (!exists)
                return HealthCheckResult.Degraded($"minio reachable, bucket `{opts.Bucket}` missing");

            return HealthCheckResult.Healthy($"minio bucket `{opts.Bucket}` ok");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"minio unreachable: {ex.Message}");
        }
    }
}
