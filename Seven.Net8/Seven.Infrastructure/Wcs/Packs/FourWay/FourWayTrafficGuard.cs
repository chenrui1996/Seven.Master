using System.Collections.Concurrent;
using Seven.Application.Interfaces;
using Seven.Infrastructure.HotStore;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

internal sealed record EdgeOccupancy(string OwnerId, string FromNode, string ToNode);

/// <summary>
/// V1 最小交通：同一无向边已被占用时拒绝对向 grant。
/// Features.HotStore 开启时写 <c>fw:flow:{edgeId}</c>；否则用进程内字典（测试/DisabledHotStore）。
/// </summary>
public sealed class FourWayTrafficGuard
{
    public const string FlowKeyPrefix = "fw:flow:";

    private readonly IHotStore? _hotStore;
    private readonly bool _useHotStore;
    private readonly ConcurrentDictionary<string, EdgeOccupancy> _fallback = new(StringComparer.OrdinalIgnoreCase);

    public FourWayTrafficGuard(IHotStore? hotStore = null)
    {
        _hotStore = hotStore;
        _useHotStore = hotStore is not null and not DisabledHotStore;
    }

    public async Task<bool> TryGrantAsync(
        string edgeId,
        string fromNode,
        string toNode,
        string ownerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromNode);
        ArgumentException.ThrowIfNullOrWhiteSpace(toNode);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var key = FlowKeyPrefix + edgeId;
        var requested = new EdgeOccupancy(ownerId, fromNode, toNode);
        return _useHotStore
            ? await TryGrantHotAsync(key, requested, ct)
            : TryGrantFallback(key, requested);
    }

    public async Task ReleaseAsync(string edgeId, string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var key = FlowKeyPrefix + edgeId;

        if (_useHotStore && _hotStore is not null)
        {
            var current = await _hotStore.GetAsync<EdgeOccupancy>(key, ct);
            if (current is not null && SameOwner(current.OwnerId, ownerId))
                await _hotStore.RemoveAsync(key, ct);
            return;
        }

        if (_fallback.TryGetValue(key, out var occ) && SameOwner(occ.OwnerId, ownerId))
            _fallback.TryRemove(key, out _);
    }

    private async Task<bool> TryGrantHotAsync(string key, EdgeOccupancy requested, CancellationToken ct)
    {
        var granted = false;
        await _hotStore!.TryUpdateAsync<EdgeOccupancy>(key, current =>
        {
            if (current is null || SameOwner(current.OwnerId, requested.OwnerId))
            {
                granted = true;
                return requested;
            }

            // Occupied: reject including head-on (A→B vs B→A on same edgeId)
            granted = false;
            return current;
        }, ct);
        return granted;
    }

    private bool TryGrantFallback(string key, EdgeOccupancy requested)
    {
        while (true)
        {
            if (_fallback.TryGetValue(key, out var current))
            {
                if (SameOwner(current.OwnerId, requested.OwnerId))
                    return true;
                // V1: same edge occupied — including head-on (A→B vs B→A)
                return false;
            }

            if (_fallback.TryAdd(key, requested))
                return true;
        }
    }

    private static bool SameOwner(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
