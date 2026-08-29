using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>堆垛机 WCS 包：入库写 PutAway，出库写 Retrieval（按总线 RefType 分流）。</summary>
public sealed class StackerWcsPack : IWcsPack
{
    private readonly SevenDbContext _db;
    private readonly IControlModeService _controlMode;
    private readonly IEquipmentTriggerPort? _port;
    private readonly StackerPathDispatcher? _pathDispatcher;
    private readonly StackerDepthGuard? _depthGuard;

    public StackerWcsPack(
        SevenDbContext db,
        IControlModeService controlMode,
        IEquipmentTriggerPort? port = null,
        StackerPathDispatcher? pathDispatcher = null,
        StackerDepthGuard? depthGuard = null)
    {
        _db = db;
        _controlMode = controlMode;
        _port = port;
        _pathDispatcher = pathDispatcher;
        _depthGuard = depthGuard;
    }

    public string PackId => "stacker";

    public Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default)
        => Task.FromResult(true);

    public async Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(leg);
        if (!await _controlMode.CanAcceptLegsAsync(PackId, ct))
            return new AcceptLegResult(false, "联锁禁止接单（急停或手动模式）");

        if (await _db.StkPutAwayTasks.AnyAsync(x => x.LegId == leg.LegId, ct)
            || await _db.StkRetrievalTasks.AnyAsync(x => x.LegId == leg.LegId, ct))
            return new AcceptLegResult(true);

        var isOutbound = string.Equals(leg.RefType, "OutboundOrder", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(leg.RefType, "StackerTransfer", StringComparison.OrdinalIgnoreCase);

        if (isOutbound)
            return await AcceptRetrievalAsync(leg, ct);

        _db.StkPutAwayTasks.Add(new StkPutAwayTask
        {
            Id = Guid.NewGuid(),
            LegId = leg.LegId,
            ContainerCode = leg.ContainerCode,
            FromCode = leg.FromCode,
            ToCode = leg.ToCode,
            Status = StkPutAwayStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return new AcceptLegResult(true);
    }

    private async Task<AcceptLegResult> AcceptRetrievalAsync(TransportLegDto leg, CancellationToken ct)
    {
        var groupNo = !string.IsNullOrWhiteSpace(leg.WcsGroupNo)
            ? leg.WcsGroupNo!
            : (leg.RefId ?? leg.OrderId.ToString("N"));
        var pri = leg.WcsPri ?? 1;

        var task = new StkRetrievalTask
        {
            Id = Guid.NewGuid(),
            LegId = leg.LegId,
            ContainerCode = leg.ContainerCode,
            FromCode = leg.FromCode,
            ToCode = leg.ToCode,
            Status = StkRetrievalStatus.Accepted,
            WcsGroupNo = groupNo,
            WcsPri = pri,
            CreateDate = DateTime.UtcNow
        };
        _db.StkRetrievalTasks.Add(task);
        await _db.SaveChangesAsync(ct);

        await TryDispatchRetrievalAsync(task, ct);
        return new AcceptLegResult(true);
    }

    /// <summary>同组仅最小 Pri 可下发；完成后由 DestinationService 滚动下一单。</summary>
    public async Task TryDispatchRetrievalAsync(StkRetrievalTask task, CancellationToken ct = default)
    {
        if (task.Status is StkRetrievalStatus.Completed or StkRetrievalStatus.Cancelled or StkRetrievalStatus.Failed)
            return;
        if (task.Status == StkRetrievalStatus.Dispatched)
            return;

        var blocked = await _db.StkRetrievalTasks.AnyAsync(x =>
            x.WcsGroupNo == task.WcsGroupNo
            && x.Id != task.Id
            && x.WcsPri < task.WcsPri
            && x.Status != StkRetrievalStatus.Completed
            && x.Status != StkRetrievalStatus.Cancelled
            && x.Status != StkRetrievalStatus.Failed, ct);
        if (blocked)
        {
            if (task.Status != StkRetrievalStatus.Suspended)
            {
                task.Status = StkRetrievalStatus.Suspended;
                task.ModifyDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return;
        }

        if (_depthGuard != null && !await _depthGuard.EnsureClearAsync(task, ct))
            return;

        if (_port == null)
            return;

        if (_pathDispatcher != null)
        {
            await _pathDispatcher.DispatchAsync(new StackerPathDispatchRequest(
                task.FromCode,
                task.ToCode,
                task.ContainerCode,
                task.LegId,
                PutAwayTaskId: null,
                task.Id), ct);
        }
        else
        {
            var device = new StkDeviceTask
            {
                Id = Guid.NewGuid(),
                RetrievalTaskId = task.Id,
                PutAwayTaskId = null,
                LegId = task.LegId,
                ContainerCode = task.ContainerCode,
                FromPointCode = task.FromCode,
                DestinationPointCode = task.ToCode,
                Status = StkDeviceTaskStatus.Dispatched,
                CreateDate = DateTime.UtcNow
            };
            _db.StkDeviceTasks.Add(device);
            await _port.DispatchDestinationAsync(
                new DispatchDestinationCommand(task.ContainerCode, task.ToCode, task.LegId), ct);
        }

        task.Status = StkRetrievalStatus.Dispatched;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelLegAsync(Guid legId, CancellationToken ct = default)
    {
        var putaway = await _db.StkPutAwayTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null)
        {
            putaway.Status = StkPutAwayStatus.Cancelled;
            putaway.ModifyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var retrieval = await _db.StkRetrievalTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (retrieval == null) return;
        retrieval.Status = StkRetrievalStatus.Cancelled;
        retrieval.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default)
    {
        var putaway = await _db.StkPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null)
            return new LegStatusDto(legId, putaway.Status.ToString());

        var retrieval = await _db.StkRetrievalTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        return retrieval == null ? null : new LegStatusDto(legId, retrieval.Status.ToString());
    }

    public async Task<PackHealthDto> HealthAsync(CancellationToken ct = default)
    {
        var canAccept = await _controlMode.CanAcceptLegsAsync(PackId, ct);
        return canAccept
            ? new PackHealthDto(PackId, true, true)
            : new PackHealthDto(PackId, false, false, "联锁禁止接单（急停或手动模式）");
    }
}
