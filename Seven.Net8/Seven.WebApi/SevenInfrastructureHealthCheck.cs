using Microsoft.Extensions.Diagnostics.HealthChecks;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Persistence;

namespace Seven.WebApi;

/// <summary>数据库 + 缓存依赖探测</summary>
public class SevenInfrastructureHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SevenInfrastructureHealthCheck(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("database unreachable");

            var key = $"health:{Guid.NewGuid():N}";
            await cache.SetAsync(key, "1", TimeSpan.FromSeconds(5), cancellationToken);
            var v = await cache.GetAsync<string>(key, cancellationToken);
            return v == "1"
                ? HealthCheckResult.Healthy("db+cache ok")
                : HealthCheckResult.Degraded("cache miss");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
