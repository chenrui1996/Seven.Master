using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>出库深浅干涉：深位出库前先移开占用浅位（TransferBin）。</summary>
public sealed class StackerDepthGuard
{
    private readonly SevenDbContext _db;
    private readonly IServiceProvider _services;

    public StackerDepthGuard(SevenDbContext db, IServiceProvider services)
    {
        _db = db;
        _services = services;
    }

    /// <summary>
    /// 若需先移浅位则创建移库运输并抬高深位 Pri，返回 false（暂缓下发）；
    /// 通道已清返回 true。
    /// </summary>
    public async Task<bool> EnsureClearAsync(StkRetrievalTask deepTask, CancellationToken ct = default)
    {
        var deepLoc = await _db.WmsLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == deepTask.FromCode, ct);
        if (deepLoc == null || StackerDoubleDeepRules.DepthValue(deepLoc) <= 1)
            return true;

        var aisle = deepLoc.Aisle;
        if (string.IsNullOrWhiteSpace(aisle))
            return true;

        var aisleLocs = await _db.WmsLocations
            .Where(x => x.PackId == WcsPackIds.Stacker && x.Aisle == aisle)
            .ToListAsync(ct);
        var profiles = await _db.StkLocationProfiles.AsNoTracking()
            .Where(x => aisleLocs.Select(l => l.Code).Contains(x.LocationCode))
            .ToListAsync(ct);
        var profileMap = profiles.ToDictionary(x => x.LocationCode, StringComparer.OrdinalIgnoreCase);

        if (profileMap.TryGetValue(deepLoc.Code, out var deepProfile) && deepProfile.OutLockBin)
            return true;

        var blockers = StackerDoubleDeepRules.ShallowBlockers(deepLoc, aisleLocs, profileMap);
        if (blockers.Count == 0)
            return true;

        var bus = _services.GetService<IOrchestrationBus>();
        if (bus == null)
            return true;

        var blocker = blockers[0];
        if (string.IsNullOrWhiteSpace(blocker.CurrentContainerCode))
            return true;

        var transferPending = await _db.StkRetrievalTasks.AnyAsync(x =>
            x.WcsGroupNo == deepTask.WcsGroupNo
            && x.Id != deepTask.Id
            && x.ContainerCode == blocker.CurrentContainerCode
            && x.Status != StkRetrievalStatus.Completed
            && x.Status != StkRetrievalStatus.Cancelled
            && x.Status != StkRetrievalStatus.Failed, ct);
        if (transferPending)
        {
            await SuspendDeepAsync(deepTask, ct);
            return false;
        }

        var transferTo = await FindTransferTargetAsync(aisle, deepLoc.Code, blocker.Code, ct);
        if (transferTo == null)
        {
            await SuspendDeepAsync(deepTask, ct);
            return false;
        }

        var transferPri = deepTask.WcsPri;
        deepTask.WcsPri = transferPri + 1;
        deepTask.Status = StkRetrievalStatus.Suspended;
        deepTask.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            blocker.CurrentContainerCode!,
            blocker.Code,
            transferTo,
            RefType: "StackerTransfer",
            RefId: deepTask.Id.ToString("N"),
            WcsGroupNo: deepTask.WcsGroupNo,
            WcsPri: transferPri), ct);

        return false;
    }

    private async Task SuspendDeepAsync(StkRetrievalTask deepTask, CancellationToken ct)
    {
        if (deepTask.Status == StkRetrievalStatus.Suspended) return;
        deepTask.Status = StkRetrievalStatus.Suspended;
        deepTask.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<string?> FindTransferTargetAsync(
        string aisle,
        string deepCode,
        string shallowCode,
        CancellationToken ct)
    {
        var free = await _db.WmsLocations
            .Where(x => x.PackId == WcsPackIds.Stacker
                        && x.Aisle == aisle
                        && x.Code != deepCode
                        && x.Code != shallowCode
                        && !x.IsOccupied && !x.IsLocked && !x.IsBooked)
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
        return free?.Code;
    }
}
