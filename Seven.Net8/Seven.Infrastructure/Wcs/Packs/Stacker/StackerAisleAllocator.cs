using Microsoft.EntityFrameworkCore;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

public sealed record AisleSelection(string AisleCode, string DestinationPointCode);

/// <summary>巷道分配：盘点锁/高重/可用/空位门槛/权重轮转。</summary>
public sealed class StackerAisleAllocator
{
    private readonly SevenDbContext _db;

    public StackerAisleAllocator(SevenDbContext db) => _db = db;

    public async Task<AisleSelection?> SelectAisleAsync(int height, int weight, CancellationToken ct = default)
    {
        var policies = await _db.StkAssignmentPolicies
            .Where(x => x.IsAvailable && height <= x.MaxHeight && weight <= x.MaxWeight)
            .ToListAsync(ct);
        if (policies.Count == 0)
            return null;

        var aisleCodes = policies.Select(x => x.AisleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var aisleRows = await _db.WmsAisles.AsNoTracking()
            .Where(x => x.PackId == WcsPackIds.Stacker && aisleCodes.Contains(x.Code))
            .ToListAsync(ct);
        var aisleByCode = aisleRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var lockedWarehouseIds = await _db.WmsWarehouses.AsNoTracking()
            .Where(x => x.IsCycleCountLocked)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var emptyCounts = await _db.WmsLocations.AsNoTracking()
            .Where(x => x.PackId == WcsPackIds.Stacker
                        && x.Aisle != null
                        && aisleCodes.Contains(x.Aisle)
                        && !x.IsOccupied && !x.IsLocked && !x.IsBooked)
            .GroupBy(x => x.Aisle!)
            .Select(g => new { Aisle = g.Key, Cnt = g.Count() })
            .ToListAsync(ct);
        var emptyByAisle = emptyCounts.ToDictionary(x => x.Aisle, x => x.Cnt, StringComparer.OrdinalIgnoreCase);

        var hasLocationMaster = await _db.WmsLocations.AsNoTracking()
            .AnyAsync(x => x.PackId == WcsPackIds.Stacker
                           && x.Aisle != null
                           && aisleCodes.Contains(x.Aisle), ct);

        var candidates = new List<StkAssignmentPolicy>();
        foreach (var p in policies)
        {
            if (aisleByCode.TryGetValue(p.AisleCode, out var aisle))
            {
                if (!aisle.IsAvailable)
                    continue;
                if (lockedWarehouseIds.Contains(aisle.WarehouseId))
                    continue;
            }

            if (hasLocationMaster && p.MinEmptySlots > 0)
            {
                emptyByAisle.TryGetValue(p.AisleCode, out var empty);
                if (empty < p.MinEmptySlots)
                    continue;
            }

            candidates.Add(p);
        }

        if (candidates.Count == 0)
            return null;

        var records = await _db.StkAssignmentRecords
            .Where(x => aisleCodes.Contains(x.AisleCode))
            .ToListAsync(ct);
        var lastByAisle = records.ToDictionary(x => x.AisleCode, StringComparer.OrdinalIgnoreCase);

        var chosen = candidates
            .OrderByDescending(p => p.AllocationWeight)
            .ThenBy(p => lastByAisle.TryGetValue(p.AisleCode, out var rec) ? rec.LastAssignedAt : DateTime.MinValue)
            .ThenBy(p => p.AisleCode, StringComparer.OrdinalIgnoreCase)
            .First();

        if (!lastByAisle.TryGetValue(chosen.AisleCode, out var record))
        {
            record = new StkAssignmentRecord
            {
                AisleCode = chosen.AisleCode,
                AssignCount = 0
            };
            _db.StkAssignmentRecords.Add(record);
        }

        record.LastAssignedAt = DateTime.UtcNow;
        record.AssignCount++;
        await _db.SaveChangesAsync(ct);

        var dest = !string.IsNullOrWhiteSpace(chosen.DestinationPointCode)
            ? chosen.DestinationPointCode
            : (aisleByCode.TryGetValue(chosen.AisleCode, out var a) ? a.EpPointCode : null)
              ?? chosen.AisleCode;
        return new AisleSelection(chosen.AisleCode, dest!);
    }
}
