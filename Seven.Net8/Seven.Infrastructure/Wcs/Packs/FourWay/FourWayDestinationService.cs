using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>订阅 SUDR/段反馈：入库分配；出库 Retrieval；跨层 Hoist；路径分段推进。</summary>
public sealed class FourWayDestinationService
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;
    private readonly FourWayInboundAllocator _allocator;
    private readonly FourWayPathDispatcher _pathDispatcher;
    private readonly IOrchestrationBus? _bus;
    private readonly FourWayWcsPack? _pack;
    private readonly FourWayHoistOrchestrator? _hoist;
    private bool _subscribed;

    public FourWayDestinationService(
        SevenDbContext db,
        IEquipmentTriggerPort port,
        FourWayInboundAllocator allocator,
        FourWayPathDispatcher pathDispatcher,
        IOrchestrationBus? bus = null,
        FourWayWcsPack? pack = null,
        FourWayHoistOrchestrator? hoist = null)
    {
        _db = db;
        _port = port;
        _allocator = allocator;
        _pathDispatcher = pathDispatcher;
        _bus = bus;
        _pack = pack;
        _hoist = hoist;
    }

    public void Subscribe()
    {
        if (_subscribed)
            return;
        _port.DestinationRequested += OnDestinationRequestedAsync;
        _port.SegmentFeedback += OnSegmentFeedbackAsync;
        _subscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_subscribed)
            return;
        _port.DestinationRequested -= OnDestinationRequestedAsync;
        _port.SegmentFeedback -= OnSegmentFeedbackAsync;
        _subscribed = false;
    }

    public Task OnDestinationRequestedAsync(DestinationRequestTrigger trigger)
        => HandleDestinationRequestedAsync(trigger);

    public Task OnSegmentFeedbackAsync(DeviceSegmentFeedback feedback)
        => HandleSegmentFeedbackAsync(feedback);

    public async Task HandleDestinationRequestedAsync(DestinationRequestTrigger trigger, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        if (!string.Equals(trigger.CheckResult, "OK", StringComparison.OrdinalIgnoreCase))
        {
            // 双包共享 TriggerPort：对端堆垛申请点的 NG 由 Stacker 处理，本包静默
            var isStackerPoint = await _db.StkRequestPoints.AsNoTracking()
                .AnyAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
            if (isStackerPoint)
                return;

            // Hoist/Shuttle 口点 NG：不误 Reject（口状态由 SegmentFeedback 推进）
            var fwNgPoint = await _db.FwRequestPoints.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
            if (fwNgPoint != null && IsHoistOrShuttlePointType(fwNgPoint.PointType))
                return;

            var failedTask = await FindActivePutAwayAsync(trigger.ContainerCode, ct);
            await RejectAsync(trigger, "外形或校验未通过: " + trigger.CheckResult, markFailed: true, failedTask, ct);
            return;
        }

        var point = await _db.FwRequestPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
        if (point == null)
            return; // 非本包申请点（可能属 Stacker），不拒收

        // Hoist*/Shuttle*：不误 Reject；推进/完成走 SegmentFeedback 编排
        if (IsHoistOrShuttlePointType(point.PointType))
            return;

        var putaway = await FindActivePutAwayAsync(trigger.ContainerCode, ct);
        if (putaway == null)
        {
            await RejectAsync(trigger, "无匹配入库上架任务", markFailed: false, ct);
            return;
        }

        string? destPoint;
        switch (point.PointType)
        {
            case FwRequestPointType.LayerRequest:
            case FwRequestPointType.AisleRequest:
            {
                destPoint = await EnsureAisleAndEpAsync(putaway, point, trigger, ct);
                if (destPoint == null)
                    return;
                break;
            }

            case FwRequestPointType.LocationRequest:
            {
                destPoint = await EnsureLocationAsync(putaway, point, trigger, ct);
                if (destPoint == null)
                    return;
                break;
            }

            default:
                await RejectAsync(trigger, "不支持的申请点类型", markFailed: false, putaway, ct);
                return;
        }

        putaway.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == putaway.LegId, ct);
        if (shuttle == null)
        {
            await RejectAsync(trigger, "无匹配穿梭任务", markFailed: true, putaway, ct);
            return;
        }

        await _pathDispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            trigger.SourcePointCode,
            destPoint!,
            putaway.ContainerCode,
            putaway.LegId,
            shuttle.Id,
            LayerCode: putaway.AssignedLayer), ct);
    }

    private static bool IsHoistOrShuttlePointType(FwRequestPointType type) =>
        type is FwRequestPointType.HoistInboundEp
            or FwRequestPointType.HoistInboundAp
            or FwRequestPointType.HoistOutboundEp
            or FwRequestPointType.HoistOutboundAp
            or FwRequestPointType.ShuttleEp
            or FwRequestPointType.ShuttleAp;

    public async Task HandleSegmentFeedbackAsync(DeviceSegmentFeedback feedback, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        if (_hoist != null && await _hoist.TryHandleSegmentFeedbackAsync(feedback, ct))
        {
            await NotifyBusIfHoistTerminalAsync(feedback, ct);
            return;
        }

        FwPutAwayTask? putaway = null;
        FwRetrievalTask? retrieval = null;
        if (feedback.LegId is { } legId)
        {
            putaway = await _db.FwPutAwayTasks.FirstOrDefaultAsync(x =>
                x.LegId == legId
                && x.Status != FwPutAwayStatus.Completed
                && x.Status != FwPutAwayStatus.Cancelled
                && x.Status != FwPutAwayStatus.Failed, ct);
            retrieval ??= await _db.FwRetrievalTasks.FirstOrDefaultAsync(x =>
                x.LegId == legId
                && x.Status != FwRetrievalStatus.Completed
                && x.Status != FwRetrievalStatus.Cancelled
                && x.Status != FwRetrievalStatus.Failed, ct);
        }

        putaway ??= await FindActivePutAwayAsync(feedback.ContainerCode, ct);
        retrieval ??= await FindActiveRetrievalAsync(feedback.ContainerCode, ct);

        if (putaway == null && retrieval == null)
            return;

        var taskLegId = putaway?.LegId ?? retrieval!.LegId;
        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == taskLegId, ct);
        if (shuttle != null
            && shuttle.Status != FwShuttleTaskStatus.Completed
            && shuttle.Status != FwShuttleTaskStatus.Cancelled
            && shuttle.Status != FwShuttleTaskStatus.Failed)
        {
            var arrived = string.IsNullOrWhiteSpace(feedback.SegmentPointCode)
                ? shuttle.ToCode
                : feedback.SegmentPointCode;
            var advanced = await _pathDispatcher.AdvanceAfterSegmentAsync(shuttle, arrived, ct);
            if (advanced)
            {
                if (retrieval != null && _pack != null)
                    await _pack.MarkParkingOccupiedAsync(retrieval.Id, ct);
                return;
            }
        }

        if (putaway != null)
        {
            putaway.Status = FwPutAwayStatus.Completed;
            putaway.ModifyDate = DateTime.UtcNow;
        }

        string? completedGroup = null;
        if (retrieval != null)
        {
            retrieval.Status = FwRetrievalStatus.Completed;
            retrieval.ModifyDate = DateTime.UtcNow;
            completedGroup = retrieval.WcsGroupNo;
            if (_pack != null)
                await _pack.ReleaseParkingAsync(retrieval.Id, ct);
        }

        if (shuttle != null
            && shuttle.Status != FwShuttleTaskStatus.Completed
            && shuttle.Status != FwShuttleTaskStatus.Cancelled
            && shuttle.Status != FwShuttleTaskStatus.Failed)
        {
            shuttle.Status = FwShuttleTaskStatus.Completed;
            shuttle.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        if (_bus != null)
            await _bus.OnLegEventAsync(new LegEvent(taskLegId, LegEventType.Completed), ct);

        if (_pack != null && !string.IsNullOrWhiteSpace(completedGroup))
            await DispatchNextInGroupAsync(completedGroup, ct);
    }

    private async Task NotifyBusIfHoistTerminalAsync(DeviceSegmentFeedback feedback, CancellationToken ct)
    {
        if (_bus == null)
            return;

        FwHoistTask? task = null;
        if (feedback.LegId is { } legId)
            task = await _db.FwHoistTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        task ??= await _db.FwHoistTasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContainerCode == feedback.ContainerCode, ct);
        if (task == null)
            return;

        if (task.Status == FwHoistTaskStatus.Completed)
            await _bus.OnLegEventAsync(new LegEvent(task.LegId, LegEventType.Completed), ct);
        else if (task.Status == FwHoistTaskStatus.Failed)
            await _bus.OnLegEventAsync(new LegEvent(task.LegId, LegEventType.Failed, Message: feedback.FeedbackCode), ct);
    }

    private async Task DispatchNextInGroupAsync(string groupNo, CancellationToken ct)
    {
        var next = await _db.FwRetrievalTasks
            .Where(x => x.WcsGroupNo == groupNo
                        && (x.Status == FwRetrievalStatus.Accepted || x.Status == FwRetrievalStatus.Suspended))
            .OrderBy(x => x.WcsPri)
            .ThenBy(x => x.CreateDate)
            .FirstOrDefaultAsync(ct);
        if (next == null) return;
        await _pack!.TryDispatchRetrievalAsync(next, ct);
    }

    private async Task<string?> EnsureAisleAndEpAsync(
        FwPutAwayTask putaway,
        FwRequestPoint point,
        DestinationRequestTrigger trigger,
        CancellationToken ct)
    {
        var warehouseId = await ResolveWarehouseIdAsync(putaway, ct);
        if (warehouseId == null)
        {
            await RejectAsync(trigger, "无法解析仓库", markFailed: true, putaway, ct);
            return null;
        }

        var request = new AllocationRequest(
            warehouseId.Value,
            WcsPackIds.FourWay,
            Height: trigger.Height,
            Weight: trigger.Weight,
            PreferredLayerCode: putaway.AssignedLayer ?? point.LayerCode,
            PreferredAisleCode: putaway.AssignedAisle ?? point.AisleCode);

        if (string.IsNullOrWhiteSpace(putaway.AssignedLayer))
        {
            var layer = await _allocator.SelectLayerForRequestAsync(request, ct);
            if (layer == null)
            {
                await RejectAsync(trigger, "层分配失败", markFailed: true, putaway, ct);
                return null;
            }

            putaway.AssignedLayer = layer;
            putaway.Status = FwPutAwayStatus.LayerAssigned;
        }

        if (string.IsNullOrWhiteSpace(putaway.AssignedAisle))
        {
            var aisleReq = request with { PreferredLayerCode = putaway.AssignedLayer };
            var aisle = await _allocator.SelectAisleForRequestAsync(aisleReq, putaway.AssignedLayer!, ct);
            if (aisle == null)
            {
                await RejectAsync(trigger, "巷道分配失败", markFailed: true, putaway, ct);
                return null;
            }

            putaway.AssignedAisle = aisle;
            putaway.Status = FwPutAwayStatus.AisleAssigned;
        }
        else if (putaway.Status is FwPutAwayStatus.Accepted or FwPutAwayStatus.LayerAssigned)
        {
            putaway.Status = FwPutAwayStatus.AisleAssigned;
        }

        var ep = await _allocator.GetAisleDestinationPointAsync(
            putaway.AssignedLayer!, putaway.AssignedAisle!, ct);
        if (string.IsNullOrWhiteSpace(ep))
        {
            await RejectAsync(trigger, "巷道缺少目的地点", markFailed: true, putaway, ct);
            return null;
        }

        return ep;
    }

    private async Task<string?> EnsureLocationAsync(
        FwPutAwayTask putaway,
        FwRequestPoint point,
        DestinationRequestTrigger trigger,
        CancellationToken ct)
    {
        var warehouseId = await ResolveWarehouseIdAsync(putaway, ct);
        if (warehouseId == null)
        {
            await RejectAsync(trigger, "无法解析仓库", markFailed: true, putaway, ct);
            return null;
        }

        var layerCode = putaway.AssignedLayer ?? point.LayerCode;
        var aisleCode = putaway.AssignedAisle ?? point.AisleCode;
        if (string.IsNullOrWhiteSpace(layerCode) || string.IsNullOrWhiteSpace(aisleCode))
        {
            await RejectAsync(trigger, "货位申请缺少层或巷道", markFailed: true, putaway, ct);
            return null;
        }

        putaway.AssignedLayer = layerCode;
        putaway.AssignedAisle = aisleCode;

        if (!string.IsNullOrWhiteSpace(putaway.AssignedLocationCode))
        {
            var prev = await _db.WmsLocations
                .FirstOrDefaultAsync(x => x.Code == putaway.AssignedLocationCode, ct);
            if (prev != null)
            {
                prev.IsBooked = false;
                prev.ModifyDate = DateTime.UtcNow;
            }

            putaway.AssignedLocationCode = null;
        }

        var location = await _allocator.SelectAndBookLocationForRequestAsync(
            warehouseId.Value, layerCode, aisleCode, ct);
        if (location == null)
        {
            await RejectAsync(trigger, "货位分配失败", markFailed: true, putaway, ct);
            return null;
        }

        putaway.AssignedLocationCode = location;
        putaway.Status = FwPutAwayStatus.LocationAssigned;
        return location;
    }

    private async Task<int?> ResolveWarehouseIdAsync(FwPutAwayTask putaway, CancellationToken ct)
    {
        foreach (var code in new[] { putaway.AssignedLocationCode, putaway.ToCode, putaway.FromCode })
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;
            var loc = await _db.WmsLocations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code, ct);
            if (loc != null)
                return loc.WarehouseId;
        }

        if (!string.IsNullOrWhiteSpace(putaway.AssignedLayer))
        {
            var layer = await _db.WmsLayers.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Code == putaway.AssignedLayer && x.PackId == WcsPackIds.FourWay, ct);
            if (layer != null)
                return layer.WarehouseId;
        }

        var wh = await _db.WmsWarehouses.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.EnabledPackIds != null
                && x.EnabledPackIds.Contains(WcsPackIds.FourWay), ct);
        return wh?.Id;
    }

    private Task<FwPutAwayTask?> FindActivePutAwayAsync(string containerCode, CancellationToken ct) =>
        _db.FwPutAwayTasks.FirstOrDefaultAsync(x => x.ContainerCode == containerCode
            && x.Status != FwPutAwayStatus.Completed
            && x.Status != FwPutAwayStatus.Cancelled
            && x.Status != FwPutAwayStatus.Failed, ct);

    private Task<FwRetrievalTask?> FindActiveRetrievalAsync(string containerCode, CancellationToken ct) =>
        _db.FwRetrievalTasks.FirstOrDefaultAsync(x => x.ContainerCode == containerCode
            && x.Status != FwRetrievalStatus.Completed
            && x.Status != FwRetrievalStatus.Cancelled
            && x.Status != FwRetrievalStatus.Failed, ct);

    private Task RejectAsync(
        DestinationRequestTrigger trigger,
        string reason,
        bool markFailed,
        CancellationToken ct) =>
        RejectAsync(trigger, reason, markFailed, putaway: null, ct);

    private async Task RejectAsync(
        DestinationRequestTrigger trigger,
        string reason,
        bool markFailed,
        FwPutAwayTask? putaway,
        CancellationToken ct)
    {
        Guid? legId = putaway?.LegId;
        if (markFailed)
        {
            if (putaway != null
                && putaway.Status != FwPutAwayStatus.Completed
                && putaway.Status != FwPutAwayStatus.Cancelled
                && putaway.Status != FwPutAwayStatus.Failed)
            {
                putaway.Status = FwPutAwayStatus.Failed;
                putaway.ModifyDate = DateTime.UtcNow;
                legId ??= putaway.LegId;
            }

            // Retrieval：同容器活动出库也标记 Failed 并释边
            var retrieval = await FindActiveRetrievalAsync(trigger.ContainerCode, ct);
            if (retrieval != null)
            {
                retrieval.Status = FwRetrievalStatus.Failed;
                retrieval.ModifyDate = DateTime.UtcNow;
                legId ??= retrieval.LegId;
                if (_pack != null)
                    await _pack.ReleaseParkingAsync(retrieval.Id, ct);
            }

            if (legId is Guid lid)
            {
                var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == lid, ct);
                if (shuttle != null
                    && shuttle.Status != FwShuttleTaskStatus.Completed
                    && shuttle.Status != FwShuttleTaskStatus.Cancelled
                    && shuttle.Status != FwShuttleTaskStatus.Failed)
                {
                    await _pathDispatcher.ReleaseAllEdgesAsync(shuttle.Id, ct);
                    shuttle.Status = FwShuttleTaskStatus.Failed;
                    shuttle.ModifyDate = DateTime.UtcNow;
                }
            }

            if (putaway != null || retrieval != null || legId != null)
                await _db.SaveChangesAsync(ct);
        }

        await _port.RejectDestinationAsync(
            new RejectDestinationCommand(trigger.ContainerCode, reason, trigger.SourcePointCode, legId), ct);
    }
}
