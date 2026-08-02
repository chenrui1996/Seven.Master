using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;

namespace Seven.Infrastructure.HotStore.Demo;

/// <summary>
/// 四向车 Demo 落库器：过滤 wcs: 前缀变更并记日志。
/// 真实项目在此批量 UPSERT 占用快照表。
/// </summary>
public sealed class WcsTrafficPersister : IHotStorePersister
{
    private readonly ILogger<WcsTrafficPersister> _logger;

    public WcsTrafficPersister(ILogger<WcsTrafficPersister> logger) => _logger = logger;

    /// <inheritdoc />
    public string Name => "WcsDemoTraffic";

    /// <inheritdoc />
    public Task PersistBatchAsync(IReadOnlyList<HotStoreChange> changes, CancellationToken cancellationToken = default)
    {
        var relevant = changes
            .Where(c => c.Key.Contains("wcs:", StringComparison.OrdinalIgnoreCase)
                        || c.Key.Contains(":wcs:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (relevant.Count == 0) return Task.CompletedTask;

        var acquires = relevant.Count(c => c.Kind == HotStoreChangeKind.Acquire);
        var releases = relevant.Count(c => c.Kind == HotStoreChangeKind.Release);
        var sets = relevant.Count(c => c.Kind == HotStoreChangeKind.Set);
        _logger.LogDebug(
            "WCS demo persist batch: total={Total}, set={Sets}, acquire={Acquires}, release={Releases}",
            relevant.Count, sets, acquires, releases);

        return Task.CompletedTask;
    }
}
