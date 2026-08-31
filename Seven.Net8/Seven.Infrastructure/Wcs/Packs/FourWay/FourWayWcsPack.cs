using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>四向车 WCS 包：入库 PutAway；出库 Retrieval；跨层 Hoist 包内多阶段。</summary>
public sealed class FourWayWcsPack : IWcsPack
{
    private readonly SevenDbContext _db;
    private readonly IControlModeService _controlMode;
    private readonly IEquipmentTriggerPort? _port;
    private readonly FourWayPathDispatcher? _pathDispatcher;
    private readonly FourWayHoistOrchestrator? _hoist;

    public FourWayWcsPack(
        SevenDbContext db,
        IControlModeService controlMode,
        IEquipmentTriggerPort? port = null,
        FourWayPathDispatcher? pathDispatcher = null,
        FourWayHoistOrchestrator? hoist = null)
    {
        _db = db;
        _controlMode = controlMode;
        _port = port;
        _pathDispatcher = pathDispatcher;
        _hoist = hoist;
    }

    public string PackId => WcsPackIds.FourWay;

    public async Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default)
    {
        var fromFw = PackCodeRules.HasValidPrefix(fromLocationCode, WcsPackIds.FourWay);
        var toFw = PackCodeRules.HasValidPrefix(toLocationCode, WcsPackIds.FourWay);
        if (fromFw && toFw)
            return true;

        var fromStk = PackCodeRules.HasValidPrefix(fromLocationCode, WcsPackIds.Stacker);
        var toStk = PackCodeRules.HasValidPrefix(toLocationCode, WcsPackIds.Stacker);
        if (fromStk && toStk)
            return false;

        var handover = await _db.WmsHandoverLinks.AsNoTracking().AnyAsync(x =>
            (x.FromPackId == WcsPackIds.FourWay || x.ToPackId == WcsPackIds.FourWay)
            && (x.LocationCode == fromLocationCode || x.LocationCode == toLocationCode), ct);
        return handover;
    }

    public async Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(leg);
        if (!await _controlMode.CanAcceptLegsAsync(PackId, ct))
            return new AcceptLegResult(false, "联锁禁止接单（急停或手动模式）");

        var hasPutAway = await _db.FwPutAwayTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        var hasRetrieval = await _db.FwRetrievalTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        var hasHoist = await _db.FwHoistTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        var hasShuttle = await _db.FwShuttleTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        if ((hasPutAway || hasRetrieval || hasHoist) && hasShuttle)
            return new AcceptLegResult(true);

        if (_hoist != null
            && !hasHoist
            && await _hoist.IsCrossLayerAsync(leg.FromCode, leg.ToCode, ct))
        {
            return await AcceptCrossLayerAsync(leg, hasShuttle, ct);
        }

        var isOutbound = string.Equals(leg.RefType, "OutboundOrder", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(leg.RefType, "FourWayTransfer", StringComparison.OrdinalIgnoreCase);

        if (isOutbound)
            return await AcceptRetrievalAsync(leg, hasShuttle, ct);

        if (!hasPutAway)
        {
            _db.FwPutAwayTasks.Add(new FwPutAwayTask
            {
                Id = Guid.NewGuid(),
                LegId = leg.LegId,
                ContainerCode = leg.ContainerCode,
                FromCode = leg.FromCode,
                ToCode = leg.ToCode,
                Status = FwPutAwayStatus.Accepted,
                CreateDate = DateTime.UtcNow
            });
        }

        if (!hasShuttle)
        {
            _db.FwShuttleTasks.Add(new FwShuttleTask
            {
                Id = Guid.NewGuid(),
                LegId = leg.LegId,
                ContainerCode = leg.ContainerCode,
                FromCode = leg.FromCode,
                ToCode = leg.ToCode,
                Status = FwShuttleTaskStatus.Accepted,
                CreateDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return new AcceptLegResult(true);
    }

    private async Task<AcceptLegResult> AcceptCrossLayerAsync(TransportLegDto leg, bool hasShuttle, CancellationToken ct)
    {
        var pair = await _hoist!.CreateHoistPairAsync(leg, ct);
        if (pair == null)
            return new AcceptLegResult(false, "跨层缺少提升机层口配置");

        FwShuttleTask shuttle;
        if (!hasShuttle)
        {
            shuttle = new FwShuttleTask
            {
                Id = Guid.NewGuid(),
                LegId = leg.LegId,
                ContainerCode = leg.ContainerCode,
                FromCode = leg.FromCode,
                ToCode = leg.ToCode,
                Status = FwShuttleTaskStatus.Accepted,
                CreateDate = DateTime.UtcNow
            };
            _db.FwShuttleTasks.Add(shuttle);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            shuttle = (await _db.FwShuttleTasks.FirstAsync(x => x.LegId == leg.LegId, ct));
        }

        if (_pathDispatcher != null && _port != null)
            await _hoist.StartToSrcApAsync(pair.Value.Task, shuttle, ct);

        return new AcceptLegResult(true);
    }

    private async Task<AcceptLegResult> AcceptRetrievalAsync(TransportLegDto leg, bool hasShuttle, CancellationToken ct)
    {
        var groupNo = !string.IsNullOrWhiteSpace(leg.WcsGroupNo)
            ? leg.WcsGroupNo!
            : (leg.RefId ?? leg.OrderId.ToString("N"));
        var pri = leg.WcsPri ?? 1;

        var task = new FwRetrievalTask
        {
            Id = Guid.NewGuid(),
            LegId = leg.LegId,
            ContainerCode = leg.ContainerCode,
            FromCode = leg.FromCode,
            ToCode = leg.ToCode,
            Status = FwRetrievalStatus.Accepted,
            WcsGroupNo = groupNo,
            WcsPri = pri,
            CreateDate = DateTime.UtcNow
        };
        _db.FwRetrievalTasks.Add(task);

        if (!hasShuttle)
        {
            _db.FwShuttleTasks.Add(new FwShuttleTask
            {
                Id = Guid.NewGuid(),
                LegId = leg.LegId,
                ContainerCode = leg.ContainerCode,
                FromCode = leg.FromCode,
                ToCode = leg.ToCode,
                Status = FwShuttleTaskStatus.Accepted,
                CreateDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        await TryDispatchRetrievalAsync(task, ct);
        return new AcceptLegResult(true);
    }

    /// <summary>同组仅最小 Pri 可下发；需预订停车位；完成后由 DestinationService 滚动下一单。</summary>
    public async Task TryDispatchRetrievalAsync(FwRetrievalTask task, CancellationToken ct = default)
    {
        if (task.Status is FwRetrievalStatus.Completed or FwRetrievalStatus.Cancelled or FwRetrievalStatus.Failed)
            return;
        if (task.Status == FwRetrievalStatus.Dispatched)
            return;

        var blocked = await _db.FwRetrievalTasks.AnyAsync(x =>
            x.WcsGroupNo == task.WcsGroupNo
            && x.Id != task.Id
            && x.WcsPri < task.WcsPri
            && x.Status != FwRetrievalStatus.Completed
            && x.Status != FwRetrievalStatus.Cancelled
            && x.Status != FwRetrievalStatus.Failed, ct);
        if (blocked)
        {
            await SuspendAsync(task, ct);
            return;
        }

        if (_port == null || _pathDispatcher == null)
            return;

        var parking = await TryReserveParkingAsync(task, ct);
        if (parking == null)
        {
            await SuspendAsync(task, ct);
            return;
        }

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == task.LegId, ct);
        if (shuttle == null)
        {
            await ReleaseParkingAsync(task.Id, ct);
            await SuspendAsync(task, ct);
            return;
        }

        string? layerCode = null;
        if (_hoist != null)
            layerCode = await _hoist.ResolveLayerCodeAsync(task.FromCode, ct)
                        ?? await _hoist.ResolveLayerCodeAsync(task.ToCode, ct);

        try
        {
            await _pathDispatcher.DispatchAsync(new FourWayPathDispatchRequest(
                task.FromCode,
                task.ToCode,
                task.ContainerCode,
                task.LegId,
                shuttle.Id,
                LayerCode: layerCode), ct);
        }
        catch
        {
            // I1：预订成功后下发异常 → 释放车位并重新挂起，避免孤儿 Reserved
            await ReleaseParkingAsync(task.Id, ct);
            try
            {
                await _pathDispatcher.ReleaseAllEdgesAsync(shuttle.Id, ct);
            }
            catch
            {
                // 释边失败不掩盖主路径清理
            }

            await SuspendAsync(task, ct);
            return;
        }

        await _db.Entry(shuttle).ReloadAsync(ct);
        // I2：仅首段占边/下发成功（Running）才 Occupied + Dispatched；Routing 保持 Reserved
        if (shuttle.Status != FwShuttleTaskStatus.Running)
            return;

        await MarkParkingOccupiedAsync(task.Id, ct);

        task.Status = FwRetrievalStatus.Dispatched;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 占边重试成功后：Reserved→Occupied，Retrieval→Dispatched（与首段成功同口径）。
    /// </summary>
    public async Task<int> PromoteRetrievalsAfterPathGrantAsync(CancellationToken ct = default)
    {
        var reservedOwnerIds = await _db.FwParkingLedgers
            .Where(x => x.Status == FwParkingStatus.Reserved && x.OwnerId != null)
            .Select(x => x.OwnerId!.Value)
            .Distinct()
            .ToListAsync(ct);
        if (reservedOwnerIds.Count == 0)
            return 0;

        var promoted = 0;
        foreach (var ownerId in reservedOwnerIds)
        {
            var retrieval = await _db.FwRetrievalTasks.FirstOrDefaultAsync(x => x.Id == ownerId, ct);
            if (retrieval == null)
                continue;
            if (retrieval.Status is FwRetrievalStatus.Completed
                or FwRetrievalStatus.Cancelled
                or FwRetrievalStatus.Failed)
                continue;

            var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == retrieval.LegId, ct);
            if (shuttle == null || shuttle.Status != FwShuttleTaskStatus.Running)
                continue;

            await MarkParkingOccupiedAsync(ownerId, ct);
            if (retrieval.Status != FwRetrievalStatus.Dispatched)
            {
                retrieval.Status = FwRetrievalStatus.Dispatched;
                retrieval.ModifyDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            promoted++;
        }

        return promoted;
    }

    /// <summary>扫描因无停车挂起的 Retrieval，有 Free 位则再派。</summary>
    public async Task<int> WakeSuspendedRetrievalsAsync(CancellationToken ct = default)
    {
        var hasFree = await _db.FwParkingLedgers.AsNoTracking()
            .AnyAsync(x => x.Status == FwParkingStatus.Free, ct);
        if (!hasFree)
            return 0;

        var suspended = await _db.FwRetrievalTasks
            .Where(x => x.Status == FwRetrievalStatus.Suspended)
            .OrderBy(x => x.WcsPri)
            .ThenBy(x => x.CreateDate)
            .Take(32)
            .ToListAsync(ct);
        if (suspended.Count == 0)
            return 0;

        var woken = 0;
        foreach (var task in suspended)
        {
            await TryDispatchRetrievalAsync(task, ct);
            if (task.Status == FwRetrievalStatus.Dispatched)
                woken++;
        }

        return woken;
    }

    public async Task ReleaseParkingAsync(Guid ownerId, CancellationToken ct = default)
    {
        var rows = await _db.FwParkingLedgers
            .Where(x => x.OwnerId == ownerId
                        && (x.Status == FwParkingStatus.Reserved || x.Status == FwParkingStatus.Occupied))
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;

        foreach (var row in rows)
        {
            row.Status = FwParkingStatus.Free;
            row.OwnerId = null;
            row.ContainerCode = null;
            row.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>首段下发成功：Reserved → Occupied。</summary>
    public async Task MarkParkingOccupiedAsync(Guid ownerId, CancellationToken ct = default)
    {
        var rows = await _db.FwParkingLedgers
            .Where(x => x.OwnerId == ownerId && x.Status == FwParkingStatus.Reserved)
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;

        foreach (var row in rows)
        {
            row.Status = FwParkingStatus.Occupied;
            row.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>原子预订：选 Free 后条件更新 WHERE Status=Free；失败则返回 null。</summary>
    private async Task<FwParkingLedger?> TryReserveParkingAsync(FwRetrievalTask task, CancellationToken ct)
    {
        var existing = await _db.FwParkingLedgers.FirstOrDefaultAsync(x =>
            x.OwnerId == task.Id
            && (x.Status == FwParkingStatus.Reserved || x.Status == FwParkingStatus.Occupied), ct);
        if (existing != null)
        {
            await EnsureParkingScopeAsync(existing, task, ct);
            return existing;
        }

        var (layerCode, aisleCode) = await ResolveParkingScopeAsync(task, parkingLocationCode: null, ct);
        var now = DateTime.UtcNow;

        if (_db.Database.IsRelational())
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var candidate = await _db.FwParkingLedgers
                    .AsNoTracking()
                    .Where(x => x.Status == FwParkingStatus.Free)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.LocationCode })
                    .FirstOrDefaultAsync(ct);
                if (candidate == null)
                    return null;

                var (stampLayer, stampAisle) = await ResolveParkingScopeAsync(
                    task, candidate.LocationCode, ct);
                stampLayer ??= layerCode;
                stampAisle ??= aisleCode;

                var affected = await _db.FwParkingLedgers
                    .Where(x => x.Id == candidate.Id && x.Status == FwParkingStatus.Free)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, FwParkingStatus.Reserved)
                        .SetProperty(x => x.OwnerId, task.Id)
                        .SetProperty(x => x.ContainerCode, task.ContainerCode)
                        .SetProperty(x => x.LayerCode, stampLayer)
                        .SetProperty(x => x.AisleCode, stampAisle)
                        .SetProperty(x => x.ModifyDate, now), ct);

                if (affected == 1)
                {
                    var tracked = _db.FwParkingLedgers.Local.FirstOrDefault(x => x.Id == candidate.Id);
                    if (tracked != null)
                        _db.Entry(tracked).State = EntityState.Detached;

                    return await _db.FwParkingLedgers.FirstAsync(x => x.Id == candidate.Id, ct);
                }
            }

            return null;
        }

        // InMemory 等非关系提供者不支持 ExecuteUpdate；进程内锁 + Status=Free 再写
        await ParkingReserveGate.WaitAsync(ct);
        try
        {
            var free = await _db.FwParkingLedgers
                .Where(x => x.Status == FwParkingStatus.Free)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(ct);
            if (free == null)
                return null;

            var (stampLayer, stampAisle) = await ResolveParkingScopeAsync(
                task, free.LocationCode, ct);
            stampLayer ??= layerCode;
            stampAisle ??= aisleCode;

            free.Status = FwParkingStatus.Reserved;
            free.OwnerId = task.Id;
            free.ContainerCode = task.ContainerCode;
            free.LayerCode = stampLayer;
            free.AisleCode = stampAisle;
            free.ModifyDate = now;
            await _db.SaveChangesAsync(ct);
            return free;
        }
        finally
        {
            ParkingReserveGate.Release();
        }
    }

    private static readonly SemaphoreSlim ParkingReserveGate = new(1, 1);

    private async Task EnsureParkingScopeAsync(
        FwParkingLedger parking,
        FwRetrievalTask task,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(parking.LayerCode) && !string.IsNullOrWhiteSpace(parking.AisleCode))
            return;

        var (layerCode, aisleCode) = await ResolveParkingScopeAsync(task, parking.LocationCode, ct);
        var changed = false;
        if (string.IsNullOrWhiteSpace(parking.LayerCode) && !string.IsNullOrWhiteSpace(layerCode))
        {
            parking.LayerCode = layerCode;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(parking.AisleCode) && !string.IsNullOrWhiteSpace(aisleCode))
        {
            parking.AisleCode = aisleCode;
            changed = true;
        }

        if (!changed)
            return;

        parking.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 解析层/巷：优先 Retrieval.FromCode 的 WmsLocation，其次 ToCode、停车 LocationCode、申请点。
    /// </summary>
    private async Task<(string? LayerCode, string? AisleCode)> ResolveParkingScopeAsync(
        FwRetrievalTask task,
        string? parkingLocationCode,
        CancellationToken ct)
    {
        string? layer = null;
        string? aisle = null;

        async Task MergeFromLocation(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;
            if (layer != null && aisle != null)
                return;

            var (l, a) = await ResolveScopeFromLocationCodeAsync(code, ct);
            layer ??= l;
            aisle ??= a;
        }

        // I3：FromCode 无库位时仍尽量从 Retrieval 相关码 / 停车位 / 申请点解析
        await MergeFromLocation(task.FromCode);
        await MergeFromLocation(task.ToCode);
        await MergeFromLocation(parkingLocationCode);

        if (layer == null || aisle == null)
        {
            foreach (var code in new[] { task.FromCode, task.ToCode, parkingLocationCode })
            {
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                var rp = await _db.FwRequestPoints.AsNoTracking()
                    .Where(x => x.Code == code && x.IsEnabled)
                    .Select(x => new { x.LayerCode, x.AisleCode })
                    .FirstOrDefaultAsync(ct);
                if (rp == null)
                    continue;

                layer ??= rp.LayerCode;
                aisle ??= rp.AisleCode;
                if (layer != null && aisle != null)
                    break;
            }
        }

        return (layer, aisle);
    }

    private async Task<(string? LayerCode, string? AisleCode)> ResolveScopeFromLocationCodeAsync(
        string code,
        CancellationToken ct)
    {
        var loc = await _db.WmsLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, ct);
        if (loc == null)
            return (null, null);

        string? layerCode = null;
        if (loc.LayerId is { } layerId)
        {
            layerCode = await _db.WmsLayers.AsNoTracking()
                .Where(x => x.Id == layerId)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(ct);
        }

        var aisleCode = loc.Aisle;
        if (string.IsNullOrWhiteSpace(aisleCode) && loc.AisleId is { } aisleId)
        {
            aisleCode = await _db.WmsAisles.AsNoTracking()
                .Where(x => x.Id == aisleId)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(ct);
        }

        return (layerCode, aisleCode);
    }

    private async Task SuspendAsync(FwRetrievalTask task, CancellationToken ct)
    {
        if (task.Status == FwRetrievalStatus.Suspended)
            return;
        task.Status = FwRetrievalStatus.Suspended;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelLegAsync(Guid legId, CancellationToken ct = default)
    {
        var putaway = await _db.FwPutAwayTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null)
        {
            putaway.Status = FwPutAwayStatus.Cancelled;
            putaway.ModifyDate = DateTime.UtcNow;
        }

        var retrieval = await _db.FwRetrievalTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (retrieval != null)
        {
            retrieval.Status = FwRetrievalStatus.Cancelled;
            retrieval.ModifyDate = DateTime.UtcNow;
            await ReleaseParkingAsync(retrieval.Id, ct);
        }

        if (_hoist != null)
            await _hoist.CancelByLegAsync(legId, ct);

        await TerminalShuttleAsync(legId, FwShuttleTaskStatus.Cancelled, ct);

        if (putaway != null || retrieval != null)
            await _db.SaveChangesAsync(ct);
    }

    /// <summary>Fail：标记 PutAway/Retrieval/Shuttle 为 Failed，并释放全部流控边。</summary>
    public async Task FailLegAsync(Guid legId, CancellationToken ct = default)
    {
        var putaway = await _db.FwPutAwayTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null
            && putaway.Status != FwPutAwayStatus.Completed
            && putaway.Status != FwPutAwayStatus.Cancelled
            && putaway.Status != FwPutAwayStatus.Failed)
        {
            putaway.Status = FwPutAwayStatus.Failed;
            putaway.ModifyDate = DateTime.UtcNow;
        }

        var retrieval = await _db.FwRetrievalTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (retrieval != null
            && retrieval.Status != FwRetrievalStatus.Completed
            && retrieval.Status != FwRetrievalStatus.Cancelled
            && retrieval.Status != FwRetrievalStatus.Failed)
        {
            retrieval.Status = FwRetrievalStatus.Failed;
            retrieval.ModifyDate = DateTime.UtcNow;
            await ReleaseParkingAsync(retrieval.Id, ct);
        }

        await TerminalShuttleAsync(legId, FwShuttleTaskStatus.Failed, ct);

        if (putaway != null || retrieval != null)
            await _db.SaveChangesAsync(ct);
    }

    private async Task TerminalShuttleAsync(Guid legId, FwShuttleTaskStatus terminal, CancellationToken ct)
    {
        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (shuttle == null)
            return;
        if (shuttle.Status is FwShuttleTaskStatus.Completed or FwShuttleTaskStatus.Cancelled or FwShuttleTaskStatus.Failed)
            return;

        if (_pathDispatcher != null)
            await _pathDispatcher.ReleaseAllEdgesAsync(shuttle.Id, ct);

        shuttle.Status = terminal;
        shuttle.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default)
    {
        var hoist = await _db.FwHoistTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (hoist != null)
            return new LegStatusDto(legId, hoist.Status.ToString());

        var putaway = await _db.FwPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null)
            return new LegStatusDto(legId, putaway.Status.ToString());

        var retrieval = await _db.FwRetrievalTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (retrieval != null)
            return new LegStatusDto(legId, retrieval.Status.ToString());

        var shuttle = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        return shuttle == null ? null : new LegStatusDto(legId, shuttle.Status.ToString());
    }

    public async Task<PackHealthDto> HealthAsync(CancellationToken ct = default)
    {
        var canAccept = await _controlMode.CanAcceptLegsAsync(PackId, ct);
        return canAccept
            ? new PackHealthDto(PackId, true, true)
            : new PackHealthDto(PackId, false, false, "联锁禁止接单（急停或手动模式）");
    }
}
