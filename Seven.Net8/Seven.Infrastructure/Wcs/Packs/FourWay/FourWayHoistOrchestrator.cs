using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>
/// 跨层提升编排：源层 Shuttle→AP → HoistExec → 目标层 Shuttle→货位；
/// 同口联锁挂起；排队 Pri/CreateTime；仿真完成复用 SegmentFeedback。
/// </summary>
public sealed class FourWayHoistOrchestrator
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;
    private readonly FourWayPathDispatcher _pathDispatcher;

    public FourWayHoistOrchestrator(
        SevenDbContext db,
        IEquipmentTriggerPort port,
        FourWayPathDispatcher pathDispatcher)
    {
        _db = db;
        _port = port;
        _pathDispatcher = pathDispatcher;
    }

    /// <summary>解析层码；无 LayerId 时返回 null。</summary>
    public async Task<string?> ResolveLayerCodeAsync(string locationCode, CancellationToken ct = default)
    {
        var loc = await _db.WmsLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == locationCode, ct);
        if (loc == null)
            return null;
        if (loc.LayerId is int lid)
        {
            var layer = await _db.WmsLayers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lid, ct);
            if (layer != null)
                return layer.Code;
        }

        return string.IsNullOrWhiteSpace(loc.Layer) ? null : loc.Layer;
    }

    public async Task<bool> IsCrossLayerAsync(string fromCode, string toCode, CancellationToken ct = default)
    {
        var src = await ResolveLayerCodeAsync(fromCode, ct);
        var des = await ResolveLayerCodeAsync(toCode, ct);
        return !string.IsNullOrWhiteSpace(src)
               && !string.IsNullOrWhiteSpace(des)
               && !string.Equals(src, des, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>按层口建 HoistTask+Exec；SrcAddress=源层 AP，DesAddress=目标层 EP。</summary>
    public async Task<(FwHoistTask Task, FwHoistExecTask Exec)?> CreateHoistPairAsync(
        TransportLegDto leg,
        CancellationToken ct = default)
    {
        var srcLayer = await ResolveLayerCodeAsync(leg.FromCode, ct);
        var desLayer = await ResolveLayerCodeAsync(leg.ToCode, ct);
        if (string.IsNullOrWhiteSpace(srcLayer) || string.IsNullOrWhiteSpace(desLayer))
            return null;

        var selected = await SelectHoistPortsAsync(srcLayer!, desLayer!, IsOutboundRefType(leg.RefType), ct);
        if (selected == null)
            return null;

        var (hoistNo, srcAp, desEp) = selected.Value;
        var pri = leg.WcsPri ?? 1;
        var task = new FwHoistTask
        {
            Id = Guid.NewGuid(),
            LegId = leg.LegId,
            ContainerCode = leg.ContainerCode,
            HoistNo = hoistNo,
            SrcLayer = srcLayer!,
            SrcAddress = srcAp,
            DesLayer = desLayer!,
            DesAddress = desEp,
            FromCode = leg.FromCode,
            ToCode = leg.ToCode,
            Status = FwHoistTaskStatus.Accepted,
            Stage = FwHoistStage.ToSrcAp,
            WcsPri = pri,
            CreateDate = DateTime.UtcNow
        };
        var exec = new FwHoistExecTask
        {
            Id = Guid.NewGuid(),
            HoistTaskId = task.Id,
            LegId = leg.LegId,
            ContainerCode = leg.ContainerCode,
            HoistNo = hoistNo,
            SrcLayer = srcLayer!,
            SrcAddress = srcAp,
            DesLayer = desLayer!,
            DesAddress = desEp,
            Status = FwHoistExecStatus.Queued,
            WcsPri = pri,
            CreateDate = DateTime.UtcNow
        };
        _db.FwHoistTasks.Add(task);
        _db.FwHoistExecTasks.Add(exec);
        await _db.SaveChangesAsync(ct);
        return (task, exec);
    }

    /// <summary>
    /// 多机选型：优先 <see cref="FwHoistDevice.IsAvailable"/> 且口空闲（无 Dispatched 同口 Exec）；
    /// 出库类 RefType 用 Outbound Ap/Ep，其它用 Inbound；空则交叉回退。
    /// </summary>
    public async Task<(string HoistNo, string SrcAp, string DesEp)?> SelectHoistPortsAsync(
        string srcLayer,
        string desLayer,
        bool outboundPorts,
        CancellationToken ct = default)
    {
        var srcPoints = await _db.FwHoistLayerPoints.AsNoTracking()
            .Where(x => x.LayerCode == srcLayer)
            .ToListAsync(ct);
        if (srcPoints.Count == 0)
            return null;

        var desPoints = await _db.FwHoistLayerPoints.AsNoTracking()
            .Where(x => x.LayerCode == desLayer)
            .ToListAsync(ct);
        if (desPoints.Count == 0)
            return null;

        var hoistNos = srcPoints.Select(x => x.HoistNo).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var devices = await _db.FwHoistDevices.AsNoTracking()
            .Where(x => hoistNos.Contains(x.HoistNo))
            .ToListAsync(ct);
        var deviceByNo = devices.ToDictionary(x => x.HoistNo, StringComparer.OrdinalIgnoreCase);

        var candidates = new List<(string HoistNo, string SrcAp, string DesEp, int Score)>();
        foreach (var src in srcPoints)
        {
            var des = desPoints.FirstOrDefault(x =>
                string.Equals(x.HoistNo, src.HoistNo, StringComparison.OrdinalIgnoreCase));
            if (des == null)
                continue;

            var srcAp = outboundPorts
                ? FirstNonEmpty(src.OutboundAp, src.InboundAp)
                : FirstNonEmpty(src.InboundAp, src.OutboundAp);
            var desEp = outboundPorts
                ? FirstNonEmpty(des.OutboundEp, des.InboundEp)
                : FirstNonEmpty(des.InboundEp, des.OutboundEp);
            if (string.IsNullOrWhiteSpace(srcAp) || string.IsNullOrWhiteSpace(desEp))
                continue;

            var score = 0;
            if (deviceByNo.TryGetValue(src.HoistNo, out var device))
            {
                if (!device.IsAvailable)
                    score -= 1000;
                else
                    score += 100;
            }
            else
            {
                score += 50; // 无台账视为可用
            }

            var busy = await _db.FwHoistExecTasks.AsNoTracking().AnyAsync(x =>
                x.HoistNo == src.HoistNo
                && x.SrcAddress == srcAp
                && x.Status == FwHoistExecStatus.Dispatched, ct);
            if (!busy)
                score += 10;

            candidates.Add((src.HoistNo, srcAp!, desEp!, score));
        }

        if (candidates.Count == 0)
            return null;

        var best = candidates.OrderByDescending(x => x.Score).ThenBy(x => x.HoistNo).First();
        return (best.HoistNo, best.SrcAp, best.DesEp);
    }

    public static bool IsOutboundRefType(string? refType) =>
        string.Equals(refType, "OutboundOrder", StringComparison.OrdinalIgnoreCase);

    /// <summary>启动/周期补扫：对 Queued/Suspended Exec 按口尝试下发。</summary>
    public async Task<int> ScanAndWakeQueuedExecsAsync(CancellationToken ct = default)
    {
        var ports = await _db.FwHoistExecTasks
            .Where(x => x.Status == FwHoistExecStatus.Queued || x.Status == FwHoistExecStatus.Suspended)
            .Select(x => new { x.HoistNo, x.SrcAddress })
            .Distinct()
            .ToListAsync(ct);

        var woken = 0;
        foreach (var p in ports)
        {
            var before = await _db.FwHoistExecTasks.CountAsync(x =>
                x.HoistNo == p.HoistNo
                && x.SrcAddress == p.SrcAddress
                && x.Status == FwHoistExecStatus.Dispatched, ct);

            await TryDispatchQueuedForPortAsync(p.HoistNo, p.SrcAddress, ct);

            var after = await _db.FwHoistExecTasks.CountAsync(x =>
                x.HoistNo == p.HoistNo
                && x.SrcAddress == p.SrcAddress
                && x.Status == FwHoistExecStatus.Dispatched, ct);
            if (after > before)
                woken += after - before;
        }

        return woken;
    }

    private static bool IsOkFeedback(DeviceSegmentFeedback feedback) =>
        string.IsNullOrWhiteSpace(feedback.FeedbackCode)
        || string.Equals(feedback.FeedbackCode, "OK", StringComparison.OrdinalIgnoreCase);

    public async Task StartToSrcApAsync(FwHoistTask task, FwShuttleTask shuttle, CancellationToken ct = default)
    {
        task.Status = FwHoistTaskStatus.Running;
        task.Stage = FwHoistStage.ToSrcAp;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _pathDispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            task.FromCode,
            task.SrcAddress,
            task.ContainerCode,
            task.LegId,
            shuttle.Id,
            LayerCode: task.SrcLayer), ct);
    }

    /// <summary>段反馈：按 Stage 推进；返回 true 表示已由 Hoist 消费。</summary>
    public async Task<bool> TryHandleSegmentFeedbackAsync(
        DeviceSegmentFeedback feedback,
        CancellationToken ct = default)
    {
        FwHoistTask? task = null;
        if (feedback.LegId is { } legId)
        {
            task = await _db.FwHoistTasks.FirstOrDefaultAsync(x =>
                x.LegId == legId
                && x.Status != FwHoistTaskStatus.Completed
                && x.Status != FwHoistTaskStatus.Cancelled
                && x.Status != FwHoistTaskStatus.Failed, ct);
        }

        task ??= await _db.FwHoistTasks.FirstOrDefaultAsync(x =>
            x.ContainerCode == feedback.ContainerCode
            && x.Status != FwHoistTaskStatus.Completed
            && x.Status != FwHoistTaskStatus.Cancelled
            && x.Status != FwHoistTaskStatus.Failed, ct);

        if (task == null)
            return false;

        return task.Stage switch
        {
            FwHoistStage.ToSrcAp => await OnArrivedSrcApAsync(task, feedback, ct),
            FwHoistStage.HoistLift => await OnHoistFeedbackAsync(task, feedback, ct),
            FwHoistStage.FromDesEp => await OnArrivedDestAsync(task, feedback, ct),
            _ => false
        };
    }

    private async Task<bool> OnArrivedSrcApAsync(
        FwHoistTask task,
        DeviceSegmentFeedback feedback,
        CancellationToken ct)
    {
        var arrived = feedback.SegmentPointCode;
        var atAp = string.IsNullOrWhiteSpace(arrived)
                   || string.Equals(arrived, task.SrcAddress, StringComparison.OrdinalIgnoreCase);
        if (!atAp)
        {
            var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == task.LegId, ct);
            if (shuttle != null)
            {
                var advanced = await _pathDispatcher.AdvanceAfterSegmentAsync(shuttle, arrived, ct);
                if (advanced)
                    return true;
            }
        }

        // FeedbackCode≠OK：不推进 Hoist 下一阶段（保持 ToSrcAp 可重试）
        if (!IsOkFeedback(feedback))
            return true;

        task.Stage = FwHoistStage.HoistLift;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var exec = await _db.FwHoistExecTasks.FirstOrDefaultAsync(x => x.HoistTaskId == task.Id, ct);
        if (exec != null)
            await TryDispatchExecAsync(exec, ct);
        return true;
    }

    private async Task<bool> OnHoistFeedbackAsync(
        FwHoistTask task,
        DeviceSegmentFeedback feedback,
        CancellationToken ct)
    {
        var exec = await _db.FwHoistExecTasks.FirstOrDefaultAsync(x =>
            x.HoistTaskId == task.Id
            && x.Status == FwHoistExecStatus.Dispatched, ct);
        if (exec == null)
            return true;

        if (!IsOkFeedback(feedback))
        {
            exec.Status = FwHoistExecStatus.Failed;
            exec.ModifyDate = DateTime.UtcNow;
            task.Status = FwHoistTaskStatus.Failed;
            task.ModifyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await TryDispatchQueuedForPortAsync(exec.HoistNo, exec.SrcAddress, ct);
            return true;
        }

        exec.Status = FwHoistExecStatus.Completed;
        exec.ModifyDate = DateTime.UtcNow;

        var device = await _db.FwHoistDevices.FirstOrDefaultAsync(x => x.HoistNo == exec.HoistNo, ct);
        if (device != null)
        {
            device.CurrentLayer = exec.DesLayer;
            device.CurrentLocation = exec.DesAddress;
            device.ModifyDate = DateTime.UtcNow;
        }

        task.Stage = FwHoistStage.FromDesEp;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == task.LegId, ct);
        if (shuttle != null)
        {
            await _pathDispatcher.DispatchAsync(new FourWayPathDispatchRequest(
                task.DesAddress,
                task.ToCode,
                task.ContainerCode,
                task.LegId,
                shuttle.Id,
                LayerCode: task.DesLayer), ct);
        }

        await TryDispatchQueuedForPortAsync(exec.HoistNo, exec.SrcAddress, ct);
        return true;
    }

    private async Task<bool> OnArrivedDestAsync(
        FwHoistTask task,
        DeviceSegmentFeedback feedback,
        CancellationToken ct)
    {
        // FeedbackCode≠OK：不推进完成（保持 FromDesEp 可重试）
        if (!IsOkFeedback(feedback))
            return true;

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == task.LegId, ct);
        if (shuttle != null
            && shuttle.Status != FwShuttleTaskStatus.Completed
            && shuttle.Status != FwShuttleTaskStatus.Cancelled
            && shuttle.Status != FwShuttleTaskStatus.Failed)
        {
            var arrived = string.IsNullOrWhiteSpace(feedback.SegmentPointCode)
                ? task.ToCode
                : feedback.SegmentPointCode;
            var advanced = await _pathDispatcher.AdvanceAfterSegmentAsync(shuttle, arrived, ct);
            if (advanced)
                return true;

            shuttle.Status = FwShuttleTaskStatus.Completed;
            shuttle.ModifyDate = DateTime.UtcNow;
        }

        task.Stage = FwHoistStage.Done;
        task.Status = FwHoistTaskStatus.Completed;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>按 Pri/CreateTime 尝试下发；同 HoistNo+SrcAddress 已有 Dispatched 则挂起。</summary>
    public async Task TryDispatchExecAsync(FwHoistExecTask exec, CancellationToken ct = default)
    {
        if (exec.Status is FwHoistExecStatus.Completed or FwHoistExecStatus.Cancelled or FwHoistExecStatus.Failed)
            return;
        if (exec.Status == FwHoistExecStatus.Dispatched)
            return;

        var conflict = await _db.FwHoistExecTasks.AnyAsync(x =>
            x.Id != exec.Id
            && x.HoistNo == exec.HoistNo
            && x.SrcAddress == exec.SrcAddress
            && x.Status == FwHoistExecStatus.Dispatched, ct);
        if (conflict)
        {
            if (exec.Status != FwHoistExecStatus.Suspended)
            {
                exec.Status = FwHoistExecStatus.Suspended;
                exec.ModifyDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return;
        }

        // 同口排队：仅当本 Exec 是 Queued/Suspended 中 Pri 最小（再 CreateTime）才下发
        var ahead = await _db.FwHoistExecTasks.AnyAsync(x =>
            x.Id != exec.Id
            && x.HoistNo == exec.HoistNo
            && x.SrcAddress == exec.SrcAddress
            && (x.Status == FwHoistExecStatus.Queued || x.Status == FwHoistExecStatus.Suspended)
            && (x.WcsPri < exec.WcsPri
                || (x.WcsPri == exec.WcsPri && x.CreateDate < exec.CreateDate)), ct);
        if (ahead)
        {
            if (exec.Status != FwHoistExecStatus.Suspended)
            {
                exec.Status = FwHoistExecStatus.Suspended;
                exec.ModifyDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return;
        }

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(exec.ContainerCode, exec.DesAddress, exec.LegId), ct);

        exec.Status = FwHoistExecStatus.Dispatched;
        exec.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task TryDispatchQueuedForPortAsync(string hoistNo, string srcAddress, CancellationToken ct = default)
    {
        var next = await _db.FwHoistExecTasks
            .Where(x => x.HoistNo == hoistNo
                        && x.SrcAddress == srcAddress
                        && (x.Status == FwHoistExecStatus.Queued || x.Status == FwHoistExecStatus.Suspended))
            .OrderBy(x => x.WcsPri)
            .ThenBy(x => x.CreateDate)
            .FirstOrDefaultAsync(ct);
        if (next == null)
            return;
        await TryDispatchExecAsync(next, ct);
    }

    public async Task CancelByLegAsync(Guid legId, CancellationToken ct = default)
    {
        var task = await _db.FwHoistTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (task != null)
        {
            task.Status = FwHoistTaskStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        var execs = await _db.FwHoistExecTasks.Where(x => x.LegId == legId).ToListAsync(ct);
        foreach (var e in execs)
        {
            if (e.Status is FwHoistExecStatus.Completed or FwHoistExecStatus.Cancelled or FwHoistExecStatus.Failed)
                continue;
            var wasDispatched = e.Status == FwHoistExecStatus.Dispatched;
            e.Status = FwHoistExecStatus.Cancelled;
            e.ModifyDate = DateTime.UtcNow;
            if (wasDispatched)
                await TryDispatchQueuedForPortAsync(e.HoistNo, e.SrcAddress, ct);
        }

        if (task != null || execs.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }
}
