using Microsoft.EntityFrameworkCore;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>货位分配：双深/LockBin/浅深优先 + Booking（对齐 LES SelectLocation）。</summary>
public sealed class StackerLocationAllocator
{
    private readonly SevenDbContext _db;

    public StackerLocationAllocator(SevenDbContext db) => _db = db;

    public async Task<string?> SelectLocationAsync(
        string aisleCode,
        int? warehouseId = null,
        bool book = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(aisleCode))
            return null;

        var query = _db.WmsLocations
            .Where(x => x.PackId == WcsPackIds.Stacker
                        && x.Aisle == aisleCode
                        && !x.IsOccupied && !x.IsLocked && !x.IsBooked);
        if (warehouseId is int wh)
            query = query.Where(x => x.WarehouseId == wh);

        var candidates = await query.ToListAsync(ct);
        if (candidates.Count == 0)
            return null;

        var aisleLocs = await _db.WmsLocations.AsNoTracking()
            .Where(x => x.PackId == WcsPackIds.Stacker && x.Aisle == aisleCode)
            .ToListAsync(ct);
        var aisleCodes = aisleLocs.Select(x => x.Code).ToList();
        var profiles = await _db.StkLocationProfiles.AsNoTracking()
            .Where(x => aisleCodes.Contains(x.LocationCode))
            .ToListAsync(ct);
        var profileMap = profiles.ToDictionary(x => x.LocationCode, StringComparer.OrdinalIgnoreCase);

        var filtered = candidates
            .Where(c =>
            {
                if (profileMap.TryGetValue(c.Code, out var p) && p.InLockBin)
                    return false;
                return StackerDoubleDeepRules.CanAllocateDeepForInbound(c, aisleLocs, profileMap);
            })
            .OrderBy(x => StackerDoubleDeepRules.DepthValue(x))
            .ThenBy(x => ParseInt(x.Layer) ?? int.MaxValue)
            .ThenBy(x => ParseInt(x.Column) ?? int.MaxValue)
            .ThenBy(x => ParseInt(x.Row) ?? int.MaxValue)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var location = filtered.FirstOrDefault();
        if (location == null)
            return null;

        if (book)
            await BookWithLockBinAsync(location.Code, profileMap, ct);

        return location.Code;
    }

    /// <summary>预约目标位；同双深组其它空位一并 IsBooked（LockBin 语义简化）。</summary>
    public async Task BookWithLockBinAsync(
        string locationCode,
        IReadOnlyDictionary<string, Domain.Entities.Wcs.Stacker.StkLocationProfile>? profileMap = null,
        CancellationToken ct = default)
    {
        var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == locationCode, ct);
        if (loc == null) return;

        loc.IsBooked = true;
        loc.ModifyDate = DateTime.UtcNow;

        profileMap ??= (await _db.StkLocationProfiles.AsNoTracking().ToListAsync(ct))
            .ToDictionary(x => x.LocationCode, StringComparer.OrdinalIgnoreCase);

        if (profileMap.TryGetValue(locationCode, out var profile)
            && !string.IsNullOrWhiteSpace(profile.BinGroupCode))
        {
            var groupCodes = profileMap
                .Where(kv => string.Equals(kv.Value.BinGroupCode, profile.BinGroupCode, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();
            var sisters = await _db.WmsLocations
                .Where(x => groupCodes.Contains(x.Code) && x.Code != locationCode && !x.IsOccupied)
                .ToListAsync(ct);
            foreach (var s in sisters)
            {
                s.IsBooked = true;
                s.ModifyDate = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var n) ? n : null;
}
