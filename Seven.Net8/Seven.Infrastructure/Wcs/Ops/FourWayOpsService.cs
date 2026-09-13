using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Packs.FourWay;

namespace Seven.Infrastructure.Wcs.Ops;

public sealed class FourWayOpsService : IFourWayOpsService
{
    private readonly SevenDbContext _db;
    private readonly IOrchestrationBus _bus;
    private readonly IControlModeService _control;
    private readonly FourWayPathDispatcher _path;
    private readonly IWcsLocationAllocatorResolver _allocators;

    public FourWayOpsService(
        SevenDbContext db,
        IOrchestrationBus bus,
        IControlModeService control,
        FourWayPathDispatcher path,
        IWcsLocationAllocatorResolver allocators)
    {
        _db = db;
        _bus = bus;
        _control = control;
        _path = path;
        _allocators = allocators;
    }

    public async Task<object> GetMetaAsync(CancellationToken ct = default)
    {
        var layers = await _db.WmsLayers.AsNoTracking()
            .Where(x => x.PackId == WcsPackIds.FourWay)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync(ct);
        var gateways = await _db.FwRequestPoints.AsNoTracking()
            .Where(x => x.IsEnabled && (x.PointType == FwRequestPointType.ShuttleEp || x.PointType == FwRequestPointType.ShuttleAp))
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.LayerCode, PointType = x.PointType.ToString() })
            .ToListAsync(ct);
        var handovers = await _db.WmsLocations.AsNoTracking()
            .Where(x => x.PackId == WcsPackIds.FourWay && x.IsHandover)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.LayerId })
            .ToListAsync(ct);
        var parking = await _db.FwParkingLedgers.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, Status = x.Status.ToString(), x.LayerCode, x.AisleCode })
            .ToListAsync(ct);
        var hoistDevices = await _db.FwHoistDevices.AsNoTracking()
            .OrderBy(x => x.HoistNo)
            .Select(x => new
            {
                code = x.HoistNo,
                x.IsAvailable,
                currentLayer = x.CurrentLayer,
                currentLocation = x.CurrentLocation
            })
            .ToListAsync(ct);
        return new
        {
            packId = WcsPackIds.FourWay,
            canAcceptLegs = await _control.CanAcceptLegsAsync(WcsPackIds.FourWay, ct),
            wcsFree = await IsWcsFreeAsync(ct),
            layers,
            gateways,
            handovers,
            parking,
            hoistDevices,
            strategies = new[] { "auto", "layer", "location" }
        };
    }

    public async Task<object> GetBoardAsync(CancellationToken ct = default)
    {
        var putAways = await _db.FwPutAwayTasks.AsNoTracking()
            .Where(x => x.Status < FwPutAwayStatus.Completed)
            .OrderByDescending(x => x.CreateDate)
            .Take(100)
            .Select(x => new
            {
                kind = "putAway",
                id = x.Id,
                x.LegId,
                x.ContainerCode,
                x.FromCode,
                x.ToCode,
                status = x.Status.ToString(),
                x.AssignedLayer,
                x.AssignedAisle,
                x.AssignedLocationCode
            })
            .ToListAsync(ct);

        var retrievals = await _db.FwRetrievalTasks.AsNoTracking()
            .Where(x => x.Status != FwRetrievalStatus.Completed
                        && x.Status != FwRetrievalStatus.Cancelled
                        && x.Status != FwRetrievalStatus.Failed)
            .OrderByDescending(x => x.CreateDate)
            .Take(100)
            .Select(x => new
            {
                kind = "retrieval",
                id = x.Id,
                x.LegId,
                x.ContainerCode,
                x.FromCode,
                x.ToCode,
                status = x.Status.ToString(),
                x.WcsGroupNo,
                x.WcsPri
            })
            .ToListAsync(ct);

        var shuttles = await _db.FwShuttleTasks.AsNoTracking()
            .Where(x => x.Status < FwShuttleTaskStatus.Completed)
            .OrderByDescending(x => x.CreateDate)
            .Take(100)
            .Select(x => new
            {
                kind = "shuttle",
                id = x.Id,
                x.LegId,
                x.ContainerCode,
                x.FromCode,
                x.ToCode,
                status = x.Status.ToString()
            })
            .ToListAsync(ct);

        var hoists = await _db.FwHoistTasks.AsNoTracking()
            .Where(x => x.Status < FwHoistTaskStatus.Completed)
            .OrderByDescending(x => x.CreateDate)
            .Take(50)
            .Select(x => new
            {
                kind = "hoist",
                id = x.Id,
                x.LegId,
                x.ContainerCode,
                status = x.Status.ToString(),
                stage = x.Stage.ToString()
            })
            .ToListAsync(ct);

        return new
        {
            putAways,
            retrievals,
            shuttles,
            hoists,
            summary = new
            {
                putAway = putAways.Count,
                retrieval = retrievals.Count,
                shuttle = shuttles.Count,
                hoist = hoists.Count,
                wcsFree = shuttles.Count == 0
            }
        };
    }

    public async Task<object?> GetTaskTreeAsync(
        Guid? shuttleTaskId, Guid? putAwayId, Guid? retrievalId, Guid? hoistTaskId, CancellationToken ct = default)
    {
        FwShuttleTask? shuttle = null;
        if (shuttleTaskId is Guid sid)
            shuttle = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sid, ct);
        else if (putAwayId is Guid pid)
        {
            var pa = await _db.FwPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pid, ct);
            if (pa != null)
                shuttle = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == pa.LegId, ct);
        }
        else if (retrievalId is Guid rid)
        {
            var rt = await _db.FwRetrievalTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rid, ct);
            if (rt != null)
                shuttle = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == rt.LegId, ct);
        }
        else if (hoistTaskId is Guid hid)
        {
            var ht = await _db.FwHoistTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == hid, ct);
            if (ht != null)
                shuttle = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == ht.LegId, ct);
        }

        if (shuttle == null) return null;

        var putAway = await _db.FwPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == shuttle.LegId, ct);
        var retrieval = await _db.FwRetrievalTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == shuttle.LegId, ct);
        var hoist = await _db.FwHoistTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == shuttle.LegId, ct);
        var paths = await _db.FwShuttleTaskPaths.AsNoTracking()
            .Where(x => x.ShuttleTaskId == shuttle.Id)
            .OrderBy(x => x.Seq)
            .Select(x => new { x.Id, x.Seq, pointCode = x.NodeCode, x.EdgeId })
            .ToListAsync(ct);
        object? hoistExecs = null;
        if (hoist != null)
        {
            hoistExecs = await _db.FwHoistExecTasks.AsNoTracking()
                .Where(x => x.HoistTaskId == hoist.Id)
                .OrderBy(x => x.WcsPri)
                .Select(x => new
                {
                    x.Id,
                    status = x.Status.ToString(),
                    fromLayerCode = x.SrcLayer,
                    toLayerCode = x.DesLayer,
                    x.SrcAddress,
                    x.DesAddress
                })
                .ToListAsync(ct);
        }

        return new
        {
            putAway = putAway == null ? null : new { putAway.Id, status = putAway.Status.ToString(), putAway.ContainerCode, putAway.FromCode, putAway.ToCode },
            retrieval = retrieval == null ? null : new { retrieval.Id, status = retrieval.Status.ToString(), retrieval.ContainerCode, retrieval.FromCode, retrieval.ToCode },
            shuttle = new
            {
                shuttle.Id,
                shuttle.LegId,
                status = shuttle.Status.ToString(),
                shuttle.ContainerCode,
                shuttle.FromCode,
                shuttle.ToCode,
                wcsFree = shuttle.Status >= FwShuttleTaskStatus.Completed,
                segmentIdle = shuttle.Status is FwShuttleTaskStatus.Accepted or FwShuttleTaskStatus.Routing
            },
            paths,
            hoist = hoist == null ? null : new { hoist.Id, status = hoist.Status.ToString(), stage = hoist.Stage.ToString() },
            hoistExecs
        };
    }

    public async Task<(bool Ok, string Message, object? Data)> CreateInboundAsync(
        FourWayOpsInboundRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ContainerCode))
            return (false, "托盘号必填", null);
        if (string.IsNullOrWhiteSpace(req.GatewayCode))
            return (false, "出入口必填", null);
        if (!await _control.CanAcceptLegsAsync(WcsPackIds.FourWay, ct))
            return (false, "联锁禁止接单", null);

        var hasOutbound = await _db.FwRetrievalTasks.AnyAsync(x =>
            x.Status != FwRetrievalStatus.Completed
            && x.Status != FwRetrievalStatus.Cancelled
            && x.Status != FwRetrievalStatus.Failed, ct);
        if (hasOutbound)
            return (false, "存在未完成出库，禁止入库（出入互斥）", null);

        var from = req.GatewayCode.Trim();
        string to;
        var strategy = (req.Strategy ?? "auto").Trim().ToLowerInvariant();

        if (strategy == "location")
        {
            if (string.IsNullOrWhiteSpace(req.LocationCode))
                return (false, "指定货位策略需要 LocationCode", null);
            var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == req.LocationCode.Trim(), ct);
            if (loc == null) return (false, "货位不存在", null);
            if (loc.IsOccupied || loc.IsBooked || loc.IsLocked)
                return (false, "货位不可选（占用/预约/锁定）", null);
            loc.IsBooked = true;
            to = loc.Code;
        }
        else
        {
            var warehouseId = await _db.WmsWarehouses.AsNoTracking()
                .Where(x => x.EnabledPackIds != null && x.EnabledPackIds.Contains(WcsPackIds.FourWay))
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);
            if (warehouseId <= 0)
                return (false, "未找到启用四向包的仓库", null);

            var allocator = _allocators.GetAllocator(WcsPackIds.FourWay);
            var allocated = await allocator.AllocateInboundAsync(
                new AllocationRequest(
                    WarehouseId: warehouseId,
                    PackId: WcsPackIds.FourWay,
                    ContainerCode: req.ContainerCode.Trim(),
                    PreferredLayerCode: strategy == "layer" ? req.LayerCode : null),
                ct);
            if (!allocated.Ok || string.IsNullOrWhiteSpace(allocated.LocationCode))
                return (false, allocated.Message ?? "无可分配货位", null);
            to = allocated.LocationCode!;
        }

        var typeId = await _db.WmsContainerTypes.AsNoTracking().Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        if (typeId is null or <= 0)
            return (false, "缺少容器类型主数据", null);

        var container = await _db.WmsContainers.FirstOrDefaultAsync(x => x.Code == req.ContainerCode.Trim(), ct);
        if (container == null)
        {
            _db.WmsContainers.Add(new WmsContainer
            {
                Code = req.ContainerCode.Trim(),
                ContainerTypeId = typeId,
                LocationCode = from,
                Status = WmsContainerStatus.Empty,
                CreateDate = DateTime.UtcNow
            });
        }
        else
        {
            container.LocationCode = from;
        }

        await _db.SaveChangesAsync(ct);

        var orderId = await _bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            ContainerCode: req.ContainerCode.Trim(),
            FromLocationCode: from,
            ToLocationCode: to,
            RefType: "FourWayOpsInbound",
            RefId: req.ContainerCode.Trim()), ct);

        return (true, "入库任务已创建", new { orderId, from, to, syncWms = req.SyncWms });
    }

    public async Task<object> GetPickableMapAsync(string? layerCode, CancellationToken ct = default)
    {
        var q = _db.WmsLocations.AsNoTracking().Where(x => x.PackId == WcsPackIds.FourWay);
        if (!string.IsNullOrWhiteSpace(layerCode))
        {
            var layer = await _db.WmsLayers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == layerCode.Trim(), ct);
            if (layer != null)
                q = q.Where(x => x.LayerId == layer.Id);
        }

        var rows = await q.OrderBy(x => x.Code).Take(2000).ToListAsync(ct);
        return rows.Select(x =>
        {
            var pickable = !x.IsOccupied && !x.IsBooked && !x.IsLocked && !x.IsHandover;
            string? reason = null;
            if (x.IsOccupied) reason = "occupied";
            else if (x.IsBooked) reason = "booked";
            else if (x.IsLocked) reason = "locked";
            else if (x.IsHandover) reason = "handover";
            return new { x.Code, x.Row, x.Column, x.Layer, x.Depth, pickable, pickBlockReason = reason };
        });
    }

    public async Task<(bool Ok, string Message, object? Data)> PointDispatchAsync(
        FourWayOpsPointDispatchRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.FromCode) || string.IsNullOrWhiteSpace(req.ToCode))
            return (false, "From/To 必填", null);
        if (!await _control.CanAcceptLegsAsync(WcsPackIds.FourWay, ct))
            return (false, "联锁禁止接单", null);
        if (!await IsWcsFreeAsync(ct))
            return (false, "WCS 非空闲，禁止指定点调度", null);

        var container = string.IsNullOrWhiteSpace(req.ContainerCode)
            ? $"OPS-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : req.ContainerCode.Trim();

        var orderId = await _bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            ContainerCode: container,
            FromLocationCode: req.FromCode.Trim(),
            ToLocationCode: req.ToCode.Trim(),
            RefType: "FourWayPointDispatch",
            RefId: container), ct);

        var leg = await _db.BusTransportLegs.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.Seq)
            .FirstOrDefaultAsync(ct);
        if (leg == null) return (true, "已建运输单", new { orderId });

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == leg.Id, ct);
        if (shuttle != null)
        {
            await _path.DispatchAsync(new FourWayPathDispatchRequest(
                FromPointCode: req.FromCode.Trim(),
                ToPointCode: req.ToCode.Trim(),
                ContainerCode: container,
                LegId: leg.Id,
                ShuttleTaskId: shuttle.Id,
                LayerCode: req.LayerCode), ct);
        }

        return (true, "指定点已下发", new { orderId, legId = leg.Id, shuttleId = shuttle?.Id });
    }

    public async Task<(bool Ok, string Message, object? Data)> ChargeAsync(
        FourWayOpsChargeRequest req, bool stop, CancellationToken ct = default)
    {
        if (stop)
        {
            var chargeLegs = await (
                from l in _db.BusTransportLegs
                join o in _db.BusTransportOrders on l.OrderId equals o.Id
                where o.RefType == "FourWayCharge"
                      && l.Status != BusLegStatus.Completed
                      && l.Status != BusLegStatus.Failed
                      && l.Status != BusLegStatus.Cancelled
                select l).ToListAsync(ct);

            foreach (var leg in chargeLegs)
            {
                var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == leg.Id, ct);
                if (shuttle != null) shuttle.Status = FwShuttleTaskStatus.Completed;
                await _bus.OnLegEventAsync(new LegEvent(leg.Id, LegEventType.Completed, "ChargeStop"), ct);
            }
            await _db.SaveChangesAsync(ct);
            return (true, "已结束充电任务", new { count = chargeLegs.Count });
        }

        if (!await IsWcsFreeAsync(ct))
            return (false, "WCS 非空闲，禁止充电调度", null);
        if (string.IsNullOrWhiteSpace(req.FromCode) || string.IsNullOrWhiteSpace(req.ChargePointCode))
            return (false, "From/ChargePoint 必填", null);

        var container = string.IsNullOrWhiteSpace(req.ContainerCode)
            ? $"CHG-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : req.ContainerCode.Trim();
        var orderId = await _bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            ContainerCode: container,
            FromLocationCode: req.FromCode.Trim(),
            ToLocationCode: req.ChargePointCode.Trim(),
            RefType: "FourWayCharge",
            RefId: container), ct);
        return (true, "充电任务已创建", new { orderId });
    }

    public async Task<(bool Ok, string Message)> ForceCompleteAsync(
        FourWayOpsForceCompleteRequest req, CancellationToken ct = default)
    {
        switch ((req.TargetType ?? "").Trim().ToLowerInvariant())
        {
            case "shuttle":
            {
                var row = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "Shuttle 不存在");
                row.Status = FwShuttleTaskStatus.Completed;
                await _path.ReleaseAllEdgesAsync(row.Id, ct);
                await ReleaseParkingForLegAsync(row.LegId, ct);
                await _db.SaveChangesAsync(ct);
                await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成穿梭任务");
            }
            case "putaway":
            {
                var row = await _db.FwPutAwayTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "PutAway 不存在");
                row.Status = FwPutAwayStatus.Completed;
                var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == row.LegId, ct);
                if (shuttle != null)
                {
                    shuttle.Status = FwShuttleTaskStatus.Completed;
                    await _path.ReleaseAllEdgesAsync(shuttle.Id, ct);
                }
                await _db.SaveChangesAsync(ct);
                await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成上架");
            }
            case "retrieval":
            {
                var row = await _db.FwRetrievalTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "Retrieval 不存在");
                row.Status = FwRetrievalStatus.Completed;
                var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == row.LegId, ct);
                if (shuttle != null)
                {
                    shuttle.Status = FwShuttleTaskStatus.Completed;
                    await _path.ReleaseAllEdgesAsync(shuttle.Id, ct);
                }
                await ReleaseParkingForOwnerAsync(row.Id, ct);
                await _db.SaveChangesAsync(ct);
                await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成取货");
            }
            case "hoist":
            {
                var row = await _db.FwHoistTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "Hoist 不存在");
                row.Status = FwHoistTaskStatus.Completed;
                row.Stage = FwHoistStage.Done;
                await _db.SaveChangesAsync(ct);
                await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成提升任务");
            }
            case "hoistexec":
            {
                var row = await _db.FwHoistExecTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "HoistExec 不存在");
                row.Status = FwHoistExecStatus.Completed;
                await _db.SaveChangesAsync(ct);
                return (true, "已强制完成提升执行段");
            }
            case "path":
                return (true, "路径点无独立状态字段，请对 Shuttle 强制完成或重发");
            default:
                return (false, "未知 TargetType");
        }
    }

    public async Task<(bool Ok, string Message)> ResendAsync(FourWayOpsResendRequest req, CancellationToken ct = default)
    {
        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.Id == req.ShuttleTaskId, ct);
        if (shuttle == null) return (false, "Shuttle 不存在");
        if (shuttle.Status is FwShuttleTaskStatus.Completed or FwShuttleTaskStatus.Cancelled or FwShuttleTaskStatus.Failed)
            return (false, "任务已终态，不能重发");
        if (shuttle.Status == FwShuttleTaskStatus.Running)
            return (false, "设备段非空闲（Running），禁止重发");

        shuttle.Status = FwShuttleTaskStatus.Routing;
        await _db.SaveChangesAsync(ct);
        await _path.DispatchAsync(new FourWayPathDispatchRequest(
            shuttle.FromCode, shuttle.ToCode, shuttle.ContainerCode, shuttle.LegId, shuttle.Id), ct);
        return (true, "已重发");
    }

    private async Task ReleaseParkingForOwnerAsync(Guid ownerId, CancellationToken ct)
    {
        var parks = await _db.FwParkingLedgers
            .Where(x => x.OwnerId == ownerId && x.Status != FwParkingStatus.Free)
            .ToListAsync(ct);
        foreach (var p in parks)
        {
            p.Status = FwParkingStatus.Free;
            p.OwnerId = null;
            p.ModifyDate = DateTime.UtcNow;
        }
    }

    private async Task ReleaseParkingForLegAsync(Guid legId, CancellationToken ct)
    {
        var retrieval = await _db.FwRetrievalTasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (retrieval != null)
            await ReleaseParkingForOwnerAsync(retrieval.Id, ct);
    }

    private Task<bool> IsWcsFreeAsync(CancellationToken ct) =>
        _db.FwShuttleTasks.AllAsync(x => x.Status >= FwShuttleTaskStatus.Completed, ct);
}
