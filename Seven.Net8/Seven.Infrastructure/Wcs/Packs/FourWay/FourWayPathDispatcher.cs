using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

public sealed record FourWayPathDispatchRequest(
    string FromPointCode,
    string ToPointCode,
    string ContainerCode,
    Guid LegId,
    Guid ShuttleTaskId,
    int? MapVersionId = null,
    string? LayerCode = null);

/// <summary>
/// 层内最短路 → <c>Fw_ShuttleTaskPath</c> → 占边 → 分段下发；无路网时单段回退（同堆垛 S2）。
/// </summary>
public sealed class FourWayPathDispatcher
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;
    private readonly FourWayTrafficGuard _traffic;

    public FourWayPathDispatcher(
        SevenDbContext db,
        IEquipmentTriggerPort port,
        FourWayTrafficGuard traffic)
    {
        _db = db;
        _port = port;
        _traffic = traffic;
    }

    public async Task<IReadOnlyList<FwShuttleTaskPath>> DispatchAsync(
        FourWayPathDispatchRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.Id == request.ShuttleTaskId, ct)
            ?? throw new InvalidOperationException($"ShuttleTask not found: {request.ShuttleTaskId}");

        var from = request.FromPointCode.Trim();
        var to = request.ToPointCode.Trim();

        var existing = await _db.FwShuttleTaskPaths
            .Where(x => x.ShuttleTaskId == request.ShuttleTaskId)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            _db.FwShuttleTaskPaths.RemoveRange(existing);
            await _db.SaveChangesAsync(ct);
        }

        var mapId = await ResolveMapVersionIdAsync(request.MapVersionId, request.LayerCode, ct);
        List<FwRoute> routes = [];
        if (mapId is int mid)
        {
            routes = await _db.FwRoutes.AsNoTracking()
                .Where(x => x.MapVersionId == mid)
                .ToListAsync(ct);
        }

        if (routes.Count == 0
            || string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return await DispatchSingleAsync(request, shuttle, from, to, ct);
        }

        var edges = routes
            .Select(r => new FourWayEdge(r.FromCode, r.ToCode, r.Weight, EdgeId: r.Id.ToString()))
            .ToList();

        var nodePath = FourWayRouter.FindPath(from, to, edges);
        if (nodePath.Count == 0)
            return await DispatchSingleAsync(request, shuttle, from, to, ct);

        var edgeByPair = routes.ToDictionary(
            r => PairKey(r.FromCode, r.ToCode),
            r => r,
            StringComparer.OrdinalIgnoreCase);

        var pathRows = new List<FwShuttleTaskPath>();
        for (var i = 0; i < nodePath.Count; i++)
        {
            string? edgeId = null;
            if (i > 0)
            {
                var key = PairKey(nodePath[i - 1], nodePath[i]);
                if (edgeByPair.TryGetValue(key, out var route))
                    edgeId = route.Id.ToString();
            }

            var row = new FwShuttleTaskPath
            {
                ShuttleTaskId = request.ShuttleTaskId,
                Seq = i + 1,
                NodeCode = nodePath[i],
                EdgeId = edgeId,
                CreateDate = DateTime.UtcNow
            };
            pathRows.Add(row);
            _db.FwShuttleTaskPaths.Add(row);
        }

        shuttle.FromCode = from;
        shuttle.ToCode = to;
        shuttle.Status = FwShuttleTaskStatus.Routing;
        shuttle.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // 首段：占边并下发第一跳目的地
        var firstHop = pathRows.FirstOrDefault(x => !string.IsNullOrEmpty(x.EdgeId));
        if (firstHop == null)
        {
            // 仅起点（from==to 已在上面回退）；防御
            return await DispatchSingleAsync(request, shuttle, from, to, ct);
        }

        var fromNode = pathRows.Single(x => x.Seq == firstHop.Seq - 1).NodeCode;
        var ownerId = request.ShuttleTaskId.ToString("N");
        // Grant 前 path 行必须已带 EdgeId（HotStore Cancel 依赖此列表）
        if (string.IsNullOrEmpty(firstHop.EdgeId))
            return await DispatchSingleAsync(request, shuttle, from, to, ct);

        var granted = await _traffic.TryGrantAsync(firstHop.EdgeId, fromNode, firstHop.NodeCode, ownerId, ct);
        if (!granted)
        {
            // 占边失败：保持 Routing，不下发（调度可稍后重试）
            return pathRows;
        }

        shuttle.Status = FwShuttleTaskStatus.Running;
        shuttle.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(request.ContainerCode, firstHop.NodeCode, request.LegId), ct);

        return pathRows;
    }

    /// <summary>
    /// 段反馈后释放当前边；若有下一段则占边并下发；否则返回 false 表示路径结束。
    /// </summary>
    public async Task<bool> AdvanceAfterSegmentAsync(
        FwShuttleTask shuttle,
        string arrivedNodeCode,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shuttle);
        ArgumentException.ThrowIfNullOrWhiteSpace(arrivedNodeCode);

        var paths = await _db.FwShuttleTaskPaths
            .Where(x => x.ShuttleTaskId == shuttle.Id)
            .OrderBy(x => x.Seq)
            .ToListAsync(ct);

        if (paths.Count == 0)
            return false;

        var arrived = paths.FirstOrDefault(x =>
            string.Equals(x.NodeCode, arrivedNodeCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (arrived == null)
            return false;

        var ownerId = shuttle.Id.ToString("N");
        if (!string.IsNullOrEmpty(arrived.EdgeId))
            await _traffic.ReleaseAsync(arrived.EdgeId, ownerId, ct);

        // 记录已到达节点，便于 Routing 重试定位待占边
        shuttle.FromCode = arrived.NodeCode;

        var next = paths.FirstOrDefault(x => x.Seq > arrived.Seq);
        if (next == null)
            return false;

        if (!string.IsNullOrEmpty(next.EdgeId))
        {
            var fromNode = paths.Single(x => x.Seq == next.Seq - 1).NodeCode;
            var granted = await _traffic.TryGrantAsync(next.EdgeId, fromNode, next.NodeCode, ownerId, ct);
            if (!granted)
            {
                shuttle.Status = FwShuttleTaskStatus.Routing;
                shuttle.ModifyDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return true; // 仍有下一段，但暂未下发
            }
        }

        shuttle.Status = FwShuttleTaskStatus.Running;
        shuttle.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(shuttle.ContainerCode, next.NodeCode, shuttle.LegId), ct);
        return true;
    }

    /// <summary>
    /// 扫描 Routing 且首跳/待跳未占边成功的穿梭任务，重试 TryGrant + Dispatch。
    /// </summary>
    public async Task<int> RetryStuckRoutingAsync(CancellationToken ct = default)
    {
        var stuck = await _db.FwShuttleTasks
            .Where(x => x.Status == FwShuttleTaskStatus.Routing)
            .ToListAsync(ct);

        var resumed = 0;
        foreach (var shuttle in stuck)
        {
            if (await TryResumeGrantAsync(shuttle, ct))
                resumed++;
        }

        return resumed;
    }

    public async Task<bool> TryResumeGrantAsync(FwShuttleTask shuttle, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shuttle);
        // 已 Running：本跳已下发，幂等跳过
        if (shuttle.Status == FwShuttleTaskStatus.Running)
            return false;
        if (shuttle.Status != FwShuttleTaskStatus.Routing)
            return false;

        var paths = await _db.FwShuttleTaskPaths
            .Where(x => x.ShuttleTaskId == shuttle.Id)
            .OrderBy(x => x.Seq)
            .ToListAsync(ct);
        if (paths.Count == 0)
            return false;

        var pending = ResolvePendingHop(paths, shuttle.FromCode);
        if (pending == null || string.IsNullOrEmpty(pending.EdgeId))
            return false;

        var fromNode = paths.Single(x => x.Seq == pending.Seq - 1).NodeCode;
        var ownerId = shuttle.Id.ToString("N");

        var alreadyHeld = await _traffic.IsHeldByOwnerAsync(pending.EdgeId, ownerId, ct);
        if (alreadyHeld)
        {
            // 边已属本车：同步 Running，不再次 Dispatch（避免 scheduler 重复下发）
            shuttle.Status = FwShuttleTaskStatus.Running;
            shuttle.ModifyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        var granted = await _traffic.TryGrantAsync(pending.EdgeId, fromNode, pending.NodeCode, ownerId, ct);
        if (!granted)
            return false;

        shuttle.Status = FwShuttleTaskStatus.Running;
        shuttle.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(shuttle.ContainerCode, pending.NodeCode, shuttle.LegId), ct);
        return true;
    }

    public async Task ReleaseCurrentEdgeAsync(Guid shuttleTaskId, string? edgeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(edgeId))
            return;
        await _traffic.ReleaseAsync(edgeId, shuttleTaskId.ToString("N"), ct);
    }

    /// <summary>
    /// Cancel/Fail：释放该穿梭任务占用的全部边。
    /// 合并 path 行 EdgeId（持久）与 TrafficGuard owner 索引（HotStore 不可枚举时的进程内跟踪）。
    /// </summary>
    public async Task ReleaseAllEdgesAsync(Guid shuttleTaskId, CancellationToken ct = default)
    {
        var edgeIds = await _db.FwShuttleTaskPaths.AsNoTracking()
            .Where(x => x.ShuttleTaskId == shuttleTaskId && x.EdgeId != null && x.EdgeId != "")
            .Select(x => x.EdgeId!)
            .ToListAsync(ct);

        await _traffic.ReleaseOwnedEdgesAsync(shuttleTaskId.ToString("N"), edgeIds, ct);
    }

    /// <summary>层码 → Active → 首条；显式 MapVersionId 优先。</summary>
    public async Task<int?> ResolveMapVersionIdAsync(
        int? preferredId,
        string? layerCode,
        CancellationToken ct = default)
    {
        if (preferredId is int id)
        {
            var exists = await _db.FwMapVersions.AsNoTracking().AnyAsync(x => x.Id == id, ct);
            if (exists)
                return id;
        }

        if (!string.IsNullOrWhiteSpace(layerCode))
        {
            var layer = layerCode.Trim();
            var byLayer = await _db.FwMapVersions.AsNoTracking()
                .Where(x => x.LayerCode == layer)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);
            if (byLayer != null)
                return byLayer;
        }

        var active = await _db.FwMapVersions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);
        if (active != null)
            return active;

        return await _db.FwMapVersions.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<IReadOnlyList<FwShuttleTaskPath>> DispatchSingleAsync(
        FourWayPathDispatchRequest request,
        FwShuttleTask shuttle,
        string from,
        string to,
        CancellationToken ct)
    {
        var rows = new List<FwShuttleTaskPath>();
        if (!string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            rows.Add(new FwShuttleTaskPath
            {
                ShuttleTaskId = request.ShuttleTaskId,
                Seq = 1,
                NodeCode = from,
                EdgeId = null,
                CreateDate = DateTime.UtcNow
            });
        }

        rows.Add(new FwShuttleTaskPath
        {
            ShuttleTaskId = request.ShuttleTaskId,
            Seq = rows.Count + 1,
            NodeCode = to,
            EdgeId = null,
            CreateDate = DateTime.UtcNow
        });

        foreach (var row in rows)
            _db.FwShuttleTaskPaths.Add(row);

        shuttle.FromCode = from;
        shuttle.ToCode = to;
        shuttle.Status = FwShuttleTaskStatus.Running;
        shuttle.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(request.ContainerCode, to, request.LegId), ct);
        return rows;
    }

    private static FwShuttleTaskPath? ResolvePendingHop(IReadOnlyList<FwShuttleTaskPath> paths, string? fromCode)
    {
        if (!string.IsNullOrWhiteSpace(fromCode))
        {
            var at = paths.FirstOrDefault(x =>
                string.Equals(x.NodeCode, fromCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (at != null)
            {
                var next = paths.FirstOrDefault(x => x.Seq > at.Seq && !string.IsNullOrEmpty(x.EdgeId));
                if (next != null)
                    return next;
            }
        }

        return paths.FirstOrDefault(x => !string.IsNullOrEmpty(x.EdgeId));
    }

    private static string PairKey(string from, string to)
        => from.Trim() + "\0" + to.Trim();
}
