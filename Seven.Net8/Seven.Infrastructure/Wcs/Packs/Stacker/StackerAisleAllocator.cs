using Microsoft.EntityFrameworkCore;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

public sealed record AisleSelection(string AisleCode, string DestinationPointCode);

/// <summary>巷道分配：按高重/可用性过滤，按 AssignmentRecord 轮转。</summary>
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

        var aisleCodes = policies.Select(x => x.AisleCode).ToList();
        var records = await _db.StkAssignmentRecords
            .Where(x => aisleCodes.Contains(x.AisleCode))
            .ToListAsync(ct);
        var lastByAisle = records.ToDictionary(x => x.AisleCode, StringComparer.OrdinalIgnoreCase);

        var chosen = policies
            .OrderBy(p => lastByAisle.TryGetValue(p.AisleCode, out var rec) ? rec.LastAssignedAt : DateTime.MinValue)
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
        return new AisleSelection(chosen.AisleCode, chosen.DestinationPointCode);
    }
}
