using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>订阅 SUDR/段反馈：入库巷道/货位分配与拒收；段完成推进多段路径；出库滚动 Pri。</summary>
public sealed class StackerDestinationService
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;
    private readonly StackerAisleAllocator _aisleAllocator;
    private readonly StackerLocationAllocator _locationAllocator;
    private readonly StackerPathDispatcher _pathDispatcher;
    private readonly IOrchestrationBus? _bus;
    private readonly StackerWcsPack? _pack;
    private bool _subscribed;

    public StackerDestinationService(
        SevenDbContext db,
        IEquipmentTriggerPort port,
        StackerAisleAllocator aisleAllocator,
        StackerLocationAllocator locationAllocator,
        StackerPathDispatcher pathDispatcher,
        IOrchestrationBus? bus = null,
        StackerWcsPack? pack = null)
    {
        _db = db;
        _port = port;
        _aisleAllocator = aisleAllocator;
        _locationAllocator = locationAllocator;
        _pathDispatcher = pathDispatcher;
        _bus = bus;
        _pack = pack;
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
            // 双包共享 TriggerPort：对端四向申请点的 NG 由 FourWay 处理，本包静默
            var isFourWayPoint = await _db.FwRequestPoints.AsNoTracking()
                .AnyAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
            if (isFourWayPoint)
                return;

            var failedTask = await FindActivePutAwayAsync(trigger.ContainerCode, ct);
            await RejectAsync(trigger, "外形或校验未通过: " + trigger.CheckResult, markFailed: true, failedTask, ct);
            return;
        }

        var point = await _db.StkRequestPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
        if (point == null)
        {
            // 双包共享 TriggerPort：本包未知但属已启用四向申请点时静默忽略，避免冲掉 FourWay 已 Dispatch
            var isFourWayPoint = await _db.FwRequestPoints.AsNoTracking()
                .AnyAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
            if (isFourWayPoint)
                return;

            await RejectAsync(trigger, "未知或未启用申请点: " + trigger.SourcePointCode, markFailed: false, ct);
            return;
        }

        var putaway = await FindActivePutAwayAsync(trigger.ContainerCode, ct);
        if (putaway == null)
        {
            await RejectAsync(trigger, "无匹配入库上架任务", markFailed: false, ct);
            return;
        }

        if (point.PointType == StkRequestPointType.BlockingPoint)
            await CancelOpenDeviceTasksAsync(putaway, ct);

        string? destPoint;
        switch (point.PointType)
        {
            case StkRequestPointType.AisleRequest:
            case StkRequestPointType.AP:
            {
                var aisle = await _aisleAllocator.SelectAisleAsync(trigger.Height, trigger.Weight, ct);
                if (aisle == null)
                {
                    await RejectAsync(trigger, "巷道分配失败", markFailed: true, putaway, ct);
                    return;
                }

                putaway.AssignedAisle = aisle.AisleCode;
                putaway.Status = StkPutAwayStatus.AisleAssigned;
                destPoint = aisle.DestinationPointCode;
                break;
            }

            case StkRequestPointType.LocationRequest:
            case StkRequestPointType.EP:
            case StkRequestPointType.BlockingPoint:
            {
                var aisleCode = putaway.AssignedAisle ?? point.AisleCode;
                if (string.IsNullOrWhiteSpace(aisleCode))
                {
                    await RejectAsync(trigger, "货位申请缺少巷道", markFailed: true, putaway, ct);
                    return;
                }

                // 阻塞重分配：释放原预约
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

                var location = await _locationAllocator.SelectLocationAsync(aisleCode, warehouseId: null, book: true, ct);
                if (location == null)
                {
                    await RejectAsync(trigger, "货位分配失败", markFailed: true, putaway, ct);
                    return;
                }

                putaway.AssignedLocationCode = location;
                putaway.Status = StkPutAwayStatus.LocationAssigned;
                destPoint = location;
                break;
            }

            default:
                await RejectAsync(trigger, "不支持的申请点类型", markFailed: false, putaway, ct);
                return;
        }

        putaway.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _pathDispatcher.DispatchAsync(new StackerPathDispatchRequest(
            trigger.SourcePointCode,
            destPoint!,
            putaway.ContainerCode,
            putaway.LegId,
            putaway.Id,
            RetrievalTaskId: null), ct);
    }

    public async Task HandleSegmentFeedbackAsync(DeviceSegmentFeedback feedback, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        // FeedbackCode≠OK：不完成段、不推进（与四向 Hoist 段反馈语义对齐；空码视为 OK）
        if (!IsOkFeedback(feedback.FeedbackCode))
            return;

        var deviceQuery = _db.StkDeviceTasks.Where(x =>
            x.Status != StkDeviceTaskStatus.Completed && x.Status != StkDeviceTaskStatus.Failed);
        if (feedback.LegId is { } legId)
            deviceQuery = deviceQuery.Where(x => x.LegId == legId);
        else
            deviceQuery = deviceQuery.Where(x => x.ContainerCode == feedback.ContainerCode);

        var device = await deviceQuery
            .Where(x => x.Status == StkDeviceTaskStatus.Dispatched)
            .OrderBy(x => x.Seq)
            .FirstOrDefaultAsync(ct);
        if (device == null)
            return;

        device.Status = StkDeviceTaskStatus.Completed;
        device.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var advanced = await _pathDispatcher.AdvanceAfterSegmentAsync(device, ct);
        if (advanced)
            return;

        string? completedGroup = null;
        if (device.PutAwayTaskId is Guid putAwayId)
        {
            var putaway = await _db.StkPutAwayTasks.FirstOrDefaultAsync(x => x.Id == putAwayId, ct);
            if (putaway != null)
            {
                putaway.Status = StkPutAwayStatus.Completed;
                putaway.ModifyDate = DateTime.UtcNow;
            }
        }
        else if (device.RetrievalTaskId is Guid retrievalId)
        {
            var retrieval = await _db.StkRetrievalTasks.FirstOrDefaultAsync(x => x.Id == retrievalId, ct);
            if (retrieval != null)
            {
                retrieval.Status = StkRetrievalStatus.Completed;
                retrieval.ModifyDate = DateTime.UtcNow;
                completedGroup = retrieval.WcsGroupNo;
            }
        }

        await _db.SaveChangesAsync(ct);

        if (_bus != null)
            await _bus.OnLegEventAsync(new LegEvent(device.LegId, LegEventType.Completed), ct);

        if (_pack != null && !string.IsNullOrWhiteSpace(completedGroup))
            await DispatchNextInGroupAsync(completedGroup, ct);
    }

    private static bool IsOkFeedback(string? code) =>
        string.IsNullOrWhiteSpace(code)
        || string.Equals(code, "OK", StringComparison.OrdinalIgnoreCase);

    private Task<StkPutAwayTask?> FindActivePutAwayAsync(string containerCode, CancellationToken ct) =>
        _db.StkPutAwayTasks.FirstOrDefaultAsync(x => x.ContainerCode == containerCode
            && x.Status != StkPutAwayStatus.Completed
            && x.Status != StkPutAwayStatus.Cancelled
            && x.Status != StkPutAwayStatus.Failed, ct);

    private async Task CancelOpenDeviceTasksAsync(StkPutAwayTask putaway, CancellationToken ct)
    {
        var open = await _db.StkDeviceTasks
            .Where(x => x.PutAwayTaskId == putaway.Id
                        && x.Status != StkDeviceTaskStatus.Completed
                        && x.Status != StkDeviceTaskStatus.Failed)
            .ToListAsync(ct);
        foreach (var d in open)
        {
            d.Status = StkDeviceTaskStatus.Failed;
            d.ModifyDate = DateTime.UtcNow;
            await _pathDispatcher.ReleaseFlowsAsync(d.Id, ct);
        }
    }

    private async Task RejectAsync(
        DestinationRequestTrigger trigger,
        string reason,
        bool markFailed,
        CancellationToken ct) =>
        await RejectAsync(trigger, reason, markFailed, putaway: null, ct);

    private async Task RejectAsync(
        DestinationRequestTrigger trigger,
        string reason,
        bool markFailed,
        StkPutAwayTask? putaway,
        CancellationToken ct)
    {
        Guid? legId = putaway?.LegId;
        if (putaway != null && markFailed)
        {
            putaway.Status = StkPutAwayStatus.Failed;
            putaway.ModifyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        await _port.RejectDestinationAsync(
            new RejectDestinationCommand(trigger.ContainerCode, reason, trigger.SourcePointCode, legId), ct);
    }

    private async Task DispatchNextInGroupAsync(string groupNo, CancellationToken ct)
    {
        var next = await _db.StkRetrievalTasks
            .Where(x => x.WcsGroupNo == groupNo
                        && (x.Status == StkRetrievalStatus.Accepted || x.Status == StkRetrievalStatus.Suspended))
            .OrderBy(x => x.WcsPri)
            .ThenBy(x => x.CreateDate)
            .FirstOrDefaultAsync(ct);
        if (next == null) return;
        await _pack!.TryDispatchRetrievalAsync(next, ct);
    }
}
