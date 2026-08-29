using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>四向车 WCS 包：接单写 Fw_ShuttleTask；寻路/交通在包内，与 Stk_ 隔离。</summary>
public sealed class FourWayWcsPack : IWcsPack
{
    private readonly SevenDbContext _db;
    private readonly IControlModeService _controlMode;

    public FourWayWcsPack(SevenDbContext db, IControlModeService controlMode)
    {
        _db = db;
        _controlMode = controlMode;
    }

    public string PackId => "fourway";

    public Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default)
        => Task.FromResult(true);

    public async Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(leg);
        if (!await _controlMode.CanAcceptLegsAsync(PackId, ct))
            return new AcceptLegResult(false, "联锁禁止接单（急停或手动模式）");

        var exists = await _db.FwShuttleTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        if (exists)
            return new AcceptLegResult(true);

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
        await _db.SaveChangesAsync(ct);
        return new AcceptLegResult(true);
    }

    public async Task CancelLegAsync(Guid legId, CancellationToken ct = default)
    {
        var task = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (task == null)
            return;
        task.Status = FwShuttleTaskStatus.Cancelled;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default)
    {
        var task = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        return task == null ? null : new LegStatusDto(legId, task.Status.ToString());
    }

    public async Task<PackHealthDto> HealthAsync(CancellationToken ct = default)
    {
        var canAccept = await _controlMode.CanAcceptLegsAsync(PackId, ct);
        return canAccept
            ? new PackHealthDto(PackId, true, true)
            : new PackHealthDto(PackId, false, false, "联锁禁止接单（急停或手动模式）");
    }
}
