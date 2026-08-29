using Microsoft.EntityFrameworkCore;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>货位分配：在指定巷道内读 Wms_Location（坐标权威），不建第二套货位表。</summary>
public sealed class StackerLocationAllocator
{
    private readonly SevenDbContext _db;

    public StackerLocationAllocator(SevenDbContext db) => _db = db;

    public async Task<string?> SelectLocationAsync(string aisleCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(aisleCode))
            return null;

        var location = await _db.WmsLocations
            .Where(x => x.Aisle == aisleCode && !x.IsOccupied && !x.IsLocked)
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
        return location?.Code;
    }
}
