using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Ops;

public sealed class StackerOpsService : IStackerOpsService
{
    private readonly SevenDbContext _db;
    private readonly IOrchestrationBus _bus;
    private readonly IEquipmentTriggerPort _port;

    public StackerOpsService(SevenDbContext db, IOrchestrationBus bus, IEquipmentTriggerPort port)
    {
        _db = db;
        _bus = bus;
        _port = port;
    }

    public async Task<object> GetBoardAsync(CancellationToken ct = default)
    {
        var putAways = await _db.StkPutAwayTasks.AsNoTracking()
            .Where(x => x.Status < StkPutAwayStatus.Completed)
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
                x.AssignedAisle,
                x.AssignedLocationCode
            })
            .ToListAsync(ct);

        var retrievals = await _db.StkRetrievalTasks.AsNoTracking()
            .Where(x => x.Status != StkRetrievalStatus.Completed
                        && x.Status != StkRetrievalStatus.Cancelled
                        && x.Status != StkRetrievalStatus.Failed)
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

        var devices = await _db.StkDeviceTasks.AsNoTracking()
            .Where(x => x.Status < StkDeviceTaskStatus.Completed)
            .OrderByDescending(x => x.CreateDate)
            .Take(200)
            .Select(x => new
            {
                kind = "device",
                id = x.Id,
                x.LegId,
                x.PutAwayTaskId,
                x.RetrievalTaskId,
                x.ContainerCode,
                x.FromPointCode,
                x.DestinationPointCode,
                x.ExeStackCode,
                x.Seq,
                status = x.Status.ToString()
            })
            .ToListAsync(ct);

        return new
        {
            putAways,
            retrievals,
            devices,
            summary = new
            {
                putAway = putAways.Count,
                retrieval = retrievals.Count,
                device = devices.Count,
                wcsFree = devices.Count == 0
            }
        };
    }

    public async Task<object?> GetTaskTreeAsync(
        Guid? putAwayId, Guid? retrievalId, Guid? deviceTaskId, CancellationToken ct = default)
    {
        Guid? legId = null;
        if (deviceTaskId is Guid did)
        {
            var d = await _db.StkDeviceTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == did, ct);
            legId = d?.LegId;
        }
        else if (putAwayId is Guid pid)
        {
            var p = await _db.StkPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pid, ct);
            legId = p?.LegId;
        }
        else if (retrievalId is Guid rid)
        {
            var r = await _db.StkRetrievalTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rid, ct);
            legId = r?.LegId;
        }

        if (legId is null) return null;

        var putAway = await _db.StkPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        var retrieval = await _db.StkRetrievalTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        var devices = await _db.StkDeviceTasks.AsNoTracking()
            .Where(x => x.LegId == legId)
            .OrderBy(x => x.Seq)
            .Select(x => new
            {
                x.Id,
                x.Seq,
                status = x.Status.ToString(),
                x.FromPointCode,
                x.DestinationPointCode,
                x.ExeStackCode,
                segmentIdle = x.Status != StkDeviceTaskStatus.Dispatched
            })
            .ToListAsync(ct);

        return new
        {
            putAway = putAway == null ? null : new { putAway.Id, status = putAway.Status.ToString(), putAway.ContainerCode, putAway.FromCode, putAway.ToCode },
            retrieval = retrieval == null ? null : new { retrieval.Id, status = retrieval.Status.ToString(), retrieval.ContainerCode, retrieval.FromCode, retrieval.ToCode },
            devices,
            wcsFree = devices.All(d => d.status is "Completed" or "Failed" or "Created")
        };
    }

    public async Task<(bool Ok, string Message)> ForceCompleteAsync(
        StackerOpsForceCompleteRequest req, CancellationToken ct = default)
    {
        switch ((req.TargetType ?? "").Trim().ToLowerInvariant())
        {
            case "putaway":
            {
                var row = await _db.StkPutAwayTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "PutAway 不存在");
                row.Status = StkPutAwayStatus.Completed;
                var devices = await _db.StkDeviceTasks.Where(x => x.LegId == row.LegId && x.Status < StkDeviceTaskStatus.Completed).ToListAsync(ct);
                foreach (var d in devices) d.Status = StkDeviceTaskStatus.Completed;
                await _db.SaveChangesAsync(ct);
                await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成上架");
            }
            case "retrieval":
            {
                var row = await _db.StkRetrievalTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "Retrieval 不存在");
                row.Status = StkRetrievalStatus.Completed;
                var devices = await _db.StkDeviceTasks.Where(x => x.LegId == row.LegId && x.Status < StkDeviceTaskStatus.Completed).ToListAsync(ct);
                foreach (var d in devices) d.Status = StkDeviceTaskStatus.Completed;
                await _db.SaveChangesAsync(ct);
                await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成取货");
            }
            case "device":
            {
                var row = await _db.StkDeviceTasks.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
                if (row == null) return (false, "DeviceTask 不存在");
                row.Status = StkDeviceTaskStatus.Completed;
                await _db.SaveChangesAsync(ct);
                var remaining = await _db.StkDeviceTasks.AnyAsync(x =>
                    x.LegId == row.LegId && x.Status < StkDeviceTaskStatus.Completed, ct);
                if (!remaining)
                    await _bus.OnLegEventAsync(new LegEvent(row.LegId, LegEventType.Completed, "ForceComplete"), ct);
                return (true, "已强制完成设备段");
            }
            default:
                return (false, "未知 TargetType");
        }
    }

    public async Task<(bool Ok, string Message)> ResendAsync(StackerOpsResendRequest req, CancellationToken ct = default)
    {
        var row = await _db.StkDeviceTasks.FirstOrDefaultAsync(x => x.Id == req.DeviceTaskId, ct);
        if (row == null) return (false, "DeviceTask 不存在");
        if (row.Status == StkDeviceTaskStatus.Dispatched)
            return (false, "设备段非空闲（已下发），禁止重发");
        if (row.Status == StkDeviceTaskStatus.Completed)
            return (false, "段已完成");

        row.Status = StkDeviceTaskStatus.Dispatched;
        await _db.SaveChangesAsync(ct);
        await _port.DispatchMoveAsync(new DispatchMoveCommand(
            row.ContainerCode, row.FromPointCode, row.DestinationPointCode, row.LegId), ct);
        return (true, "已重发设备段");
    }

    public async Task<object> ListRequestPointsAsync(CancellationToken ct = default) =>
        await _db.StkRequestPoints.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                pointType = x.PointType.ToString(),
                x.IsEnabled,
                x.AisleCode
            })
            .ToListAsync(ct);

    public async Task<(bool Ok, string Message)> SetRequestPointEnabledAsync(int id, bool enabled, CancellationToken ct = default)
    {
        var row = await _db.StkRequestPoints.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row == null) return (false, "申请点不存在");
        row.IsEnabled = enabled;
        await _db.SaveChangesAsync();
        return (true, enabled ? "已启用" : "已停用");
    }
}
