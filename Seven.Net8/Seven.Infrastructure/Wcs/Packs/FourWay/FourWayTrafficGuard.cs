using System.Collections.Concurrent;
using Seven.Application.Interfaces;
using Seven.Infrastructure.HotStore;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

internal sealed record EdgeOccupancy(string OwnerId, string FromNode, string ToNode);

/// <summary>
/// V1 最小交通：同一无向边已被占用时拒绝对向 grant。
/// Features.HotStore 开启时写 <c>fw:flow:{edgeId}</c>；否则用进程内字典（测试/DisabledHotStore）。
/// <para>
/// HotStore 无法枚举 key，故另维护进程内 owner→edgeIds 索引；
/// <see cref="ReleaseAllForOwnerAsync"/> 依赖该索引 + 调用方传入的 path EdgeId。
/// 约定：每次成功 Grant 的边必须写入 path 行 EdgeId，Cancel/Fail 释放这些 EdgeId。
/// </para>
/// </summary>
public sealed class FourWayTrafficGuard
{
    public const string FlowKeyPrefix = "fw:flow:";

    private readonly IHotStore? _hotStore;
    private readonly bool _useHotStore;
    private readonly ConcurrentDictionary<string, EdgeOccupancy> _fallback = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>ownerId → edgeId 集合（HotStore 与 fallback 共用；弥补 HotStore 不可枚举）。</summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _ownerEdges =
        new(StringComparer.OrdinalIgnoreCase);

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
        var granted = _useHotStore
            ? await TryGrantHotAsync(key, requested, ct)
            : TryGrantFallback(key, requested);

        if (granted)
            TrackOwnerEdge(ownerId, edgeId);

        return granted;
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
            {
                await _hotStore.RemoveAsync(key, ct);
                UntrackOwnerEdge(ownerId, edgeId);
            }
            return;
        }

        if (_fallback.TryGetValue(key, out var occ) && SameOwner(occ.OwnerId, ownerId))
        {
            _fallback.TryRemove(key, out _);
            UntrackOwnerEdge(ownerId, edgeId);
        }
    }

    /// <summary>按已知边列表释放该 owner 占用（Cancel/Fail 清理）；并合并 owner 索引中的边。</summary>
    public async Task ReleaseOwnedEdgesAsync(
        string ownerId,
        IEnumerable<string> edgeIds,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edgeId in edgeIds)
        {
            if (!string.IsNullOrWhiteSpace(edgeId))
                ids.Add(edgeId);
        }

        if (_ownerEdges.TryGetValue(ownerId, out var tracked))
        {
            foreach (var edgeId in tracked.Keys)
                ids.Add(edgeId);
        }

        foreach (var edgeId in ids)
            await ReleaseAsync(edgeId, ownerId, ct);

        // 进程内 fallback：再扫一遍残留同 owner 键
        if (!_useHotStore)
        {
            foreach (var kv in _fallback.ToArray())
            {
                if (SameOwner(kv.Value.OwnerId, ownerId))
                    _fallback.TryRemove(kv.Key, out _);
            }
        }

        _ownerEdges.TryRemove(ownerId, out _);
    }

    /// <summary>
    /// 释放该 owner 全部已知占用。HotStore 无法 SCAN，依赖 grant 时写入的 owner 索引；
    /// 与 path EdgeId 列表一并使用最稳妥。
    /// </summary>
    public Task ReleaseAllForOwnerAsync(string ownerId, CancellationToken ct = default)
        => ReleaseOwnedEdgesAsync(ownerId, Array.Empty<string>(), ct);

    public async Task<bool> IsHeldByOwnerAsync(string edgeId, string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var key = FlowKeyPrefix + edgeId;

        if (_useHotStore && _hotStore is not null)
        {
            var current = await _hotStore.GetAsync<EdgeOccupancy>(key, ct);
            return current is not null && SameOwner(current.OwnerId, ownerId);
        }

        return _fallback.TryGetValue(key, out var occ) && SameOwner(occ.OwnerId, ownerId);
    }

    /// <summary>测试/诊断：该 owner 是否仍占任意 fallback 流键或 owner 索引。</summary>
    public bool HasAnyFallbackHold(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            return false;
        if (_fallback.Values.Any(x => SameOwner(x.OwnerId, ownerId)))
            return true;
        return _ownerEdges.TryGetValue(ownerId, out var tracked) && !tracked.IsEmpty;
    }

    private void TrackOwnerEdge(string ownerId, string edgeId)
    {
        var set = _ownerEdges.GetOrAdd(ownerId, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        set[edgeId] = 0;
    }

    private void UntrackOwnerEdge(string ownerId, string edgeId)
    {
        if (!_ownerEdges.TryGetValue(ownerId, out var set))
            return;
        set.TryRemove(edgeId, out _);
        if (set.IsEmpty)
            _ownerEdges.TryRemove(ownerId, out _);
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
