# Review package Task 2

## Files
- Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayInboundAllocator.cs
- Seven.Net8/Seven.Tests/Wcs/FourWayAllocatorTests.cs


### FILE: Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayInboundAllocator.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>鍥涘悜鍏ュ簱鍒嗛厤锛歋electLayer 鈫?SelectAisle 鈫?SelectLocation + Booking銆?/summary>
public sealed class FourWayInboundAllocator : IWcsLocationAllocator
{
    private readonly SevenDbContext _db;

    public FourWayInboundAllocator(SevenDbContext db) => _db = db;

    public string PackId => WcsPackIds.FourWay;

    public async Task<AllocationResult> AllocateInboundAsync(AllocationRequest request, CancellationToken ct = default)
    {
        var stages = new List<AllocationStageResult>();
        var height = (int)request.Height;
        var weight = request.Weight;

        var layerCode = await ResolveLayerAsync(request, height, weight, ct);
        if (layerCode == null)
            return new AllocationResult(false, null, null, null, "鏃犲彲鐢ㄥ眰绛栫暐", stages);
        stages.Add(new AllocationStageResult("Layer", layerCode));

        var aisleCode = await ResolveAisleAsync(request, layerCode, height, weight, ct);
        if (aisleCode == null)
            return new AllocationResult(false, null, layerCode, null, "鏃犲彲鐢ㄥ贩閬撶瓥鐣?, stages);
        stages.Add(new AllocationStageResult("Aisle", aisleCode));

        var locCode = await SelectAndBookLocationAsync(request.WarehouseId, layerCode, aisleCode, ct);
        if (locCode == null)
            return new AllocationResult(false, null, layerCode, aisleCode, "宸烽亾鍐呮棤绌洪棽璐т綅", stages);
        stages.Add(new AllocationStageResult("Location", locCode));

        return new AllocationResult(true, locCode, layerCode, aisleCode, null, stages);
    }

    private async Task<string?> ResolveLayerAsync(
        AllocationRequest request,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.PreferredLayerCode))
        {
            var preferred = request.PreferredLayerCode.Trim();
            var layer = await _db.WmsLayers.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == request.WarehouseId
                    && x.PackId == WcsPackIds.FourWay
                    && x.Code == preferred, ct);
            if (layer == null)
                return null;
            if (!layer.IsAvailable)
                return null;
            await TouchAssignmentAsync(FwAssignmentScopeType.Layer, preferred, ct);
            return preferred;
        }

        return await SelectLayerAsync(request, height, weight, ct);
    }

    private async Task<string?> SelectLayerAsync(
        AllocationRequest request,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        var warehouse = await _db.WmsWarehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.WarehouseId, ct);
        if (warehouse == null)
            return null;

        var policies = await _db.FwLayerPolicies
            .Where(x => x.IsAvailable
                        && height <= x.MaxHeight
                        && weight <= x.MaxWeight
                        && x.WarehouseCode == warehouse.Code)
            .ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.ZoneCode))
            policies = policies.Where(x => x.ZoneCode == request.ZoneCode).ToList();
        if (policies.Count == 0)
            return null;

        var layerCodes = policies.Select(x => x.LayerCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var layerRows = await _db.WmsLayers.AsNoTracking()
            .Where(x => x.WarehouseId == request.WarehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && layerCodes.Contains(x.Code))
            .ToListAsync(ct);
        var layerByCode = layerRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var hasLayerMaster = layerRows.Count > 0;

        var candidates = new List<FwLayerPolicy>();
        foreach (var p in policies)
        {
            if (hasLayerMaster || layerByCode.ContainsKey(p.LayerCode))
            {
                if (layerByCode.TryGetValue(p.LayerCode, out var layer) && !layer.IsAvailable)
                    continue;
                if (hasLayerMaster && !layerByCode.ContainsKey(p.LayerCode))
                    continue;
            }

            candidates.Add(p);
        }

        if (candidates.Count == 0)
            return null;

        var records = await _db.FwAssignmentRecords
            .Where(x => x.ScopeType == FwAssignmentScopeType.Layer && layerCodes.Contains(x.ScopeCode))
            .ToListAsync(ct);
        var lastByLayer = records.ToDictionary(x => x.ScopeCode, StringComparer.OrdinalIgnoreCase);

        var chosen = candidates
            .OrderByDescending(p => p.AllocationWeight)
            .ThenBy(p => lastByLayer.TryGetValue(p.LayerCode, out var rec) ? rec.LastAssignedAt : DateTime.MinValue)
            .ThenBy(p => p.LayerCode, StringComparer.OrdinalIgnoreCase)
            .First();

        await TouchAssignmentAsync(FwAssignmentScopeType.Layer, chosen.LayerCode, ct);
        return chosen.LayerCode;
    }

    private async Task<string?> ResolveAisleAsync(
        AllocationRequest request,
        string layerCode,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.PreferredAisleCode))
        {
            var preferred = request.PreferredAisleCode.Trim();
            await TouchAssignmentAsync(FwAssignmentScopeType.Aisle, AisleScopeCode(layerCode, preferred), ct);
            return preferred;
        }

        var hasAislePolicy = await _db.FwAislePolicies.AsNoTracking()
            .AnyAsync(x => x.LayerCode == layerCode, ct);
        if (hasAislePolicy)
            return await SelectAisleAsync(request.WarehouseId, layerCode, height, weight, ct);

        // 鏃犲贩绛栫暐鏃讹細浠庡眰鍐呯┖闂茶揣浣嶆帹瀵煎贩閬擄紙鍏煎 PreferredLayer 浠呮寚瀹氬眰锛?
        return await FallbackAisleFromFreeLocationsAsync(request.WarehouseId, layerCode, ct);
    }

    private async Task<string?> FallbackAisleFromFreeLocationsAsync(
        int warehouseId,
        string layerCode,
        CancellationToken ct)
    {
        var layer = await _db.WmsLayers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId
                && x.PackId == WcsPackIds.FourWay
                && x.Code == layerCode, ct);

        var query = _db.WmsLocations.AsNoTracking().Where(x =>
            x.WarehouseId == warehouseId
            && x.PackId == WcsPackIds.FourWay
            && x.Aisle != null
            && !x.IsOccupied && !x.IsLocked && !x.IsBooked);
        if (layer != null)
            query = query.Where(x => x.LayerId == null || x.LayerId == layer.Id);

        var aisle = await query.OrderBy(x => x.Code).Select(x => x.Aisle).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(aisle))
            return null;

        await TouchAssignmentAsync(FwAssignmentScopeType.Aisle, AisleScopeCode(layerCode, aisle), ct);
        return aisle;
    }

    private async Task<string?> SelectAisleAsync(
        int warehouseId,
        string layerCode,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        var policies = await _db.FwAislePolicies
            .Where(x => x.LayerCode == layerCode
                        && x.IsAvailable
                        && height <= x.MaxHeight
                        && weight <= x.MaxWeight)
            .ToListAsync(ct);
        if (policies.Count == 0)
            return null;

        var aisleCodes = policies.Select(x => x.AisleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var aisleRows = await _db.WmsAisles.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && aisleCodes.Contains(x.Code))
            .ToListAsync(ct);
        var aisleByCode = aisleRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var emptyCounts = await _db.WmsLocations.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && x.Aisle != null
                        && aisleCodes.Contains(x.Aisle)
                        && !x.IsOccupied && !x.IsLocked && !x.IsBooked)
            .GroupBy(x => x.Aisle!)
            .Select(g => new { Aisle = g.Key, Cnt = g.Count() })
            .ToListAsync(ct);
        var emptyByAisle = emptyCounts.ToDictionary(x => x.Aisle, x => x.Cnt, StringComparer.OrdinalIgnoreCase);

        var hasLocationMaster = await _db.WmsLocations.AsNoTracking()
            .AnyAsync(x => x.WarehouseId == warehouseId
                           && x.PackId == WcsPackIds.FourWay
                           && x.Aisle != null
                           && aisleCodes.Contains(x.Aisle), ct);

        var candidates = new List<FwAislePolicy>();
        foreach (var p in policies)
        {
            if (aisleByCode.TryGetValue(p.AisleCode, out var aisle) && !aisle.IsAvailable)
                continue;

            if (hasLocationMaster && p.MinEmptySlots > 0)
            {
                emptyByAisle.TryGetValue(p.AisleCode, out var empty);
                if (empty < p.MinEmptySlots)
                    continue;
            }

            // MaxShuttleCount > 0 棰勭暀锛氬綋鍓嶆棤绌挎杞﹀崰鐢ㄨ处鏈椂涓嶆嫤鎴紙F5 鍋滆溅璐︽湰鍚庡啀鏀剁揣锛?
            candidates.Add(p);
        }

        if (candidates.Count == 0)
            return null;

        var scopeCodes = aisleCodes.Select(a => AisleScopeCode(layerCode, a)).ToList();
        var records = await _db.FwAssignmentRecords
            .Where(x => x.ScopeType == FwAssignmentScopeType.Aisle && scopeCodes.Contains(x.ScopeCode))
            .ToListAsync(ct);
        var lastByScope = records.ToDictionary(x => x.ScopeCode, StringComparer.OrdinalIgnoreCase);

        var chosen = candidates
            .OrderByDescending(p => p.AllocationWeight)
            .ThenBy(p =>
            {
                var key = AisleScopeCode(layerCode, p.AisleCode);
                return lastByScope.TryGetValue(key, out var rec) ? rec.LastAssignedAt : DateTime.MinValue;
            })
            .ThenBy(p => p.AisleCode, StringComparer.OrdinalIgnoreCase)
            .First();

        await TouchAssignmentAsync(FwAssignmentScopeType.Aisle, AisleScopeCode(layerCode, chosen.AisleCode), ct);
        return chosen.AisleCode;
    }

    private async Task<string?> SelectAndBookLocationAsync(
        int warehouseId,
        string layerCode,
        string aisleCode,
        CancellationToken ct)
    {
        var layer = await _db.WmsLayers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId
                && x.PackId == WcsPackIds.FourWay
                && x.Code == layerCode, ct);

        var query = _db.WmsLocations.Where(x =>
            x.WarehouseId == warehouseId
            && x.PackId == WcsPackIds.FourWay
            && x.Aisle == aisleCode
            && !x.IsOccupied && !x.IsLocked && !x.IsBooked);

        if (layer != null)
            query = query.Where(x => x.LayerId == null || x.LayerId == layer.Id);

        var loc = await query
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
        if (loc == null)
            return null;

        loc.IsBooked = true;
        loc.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return loc.Code;
    }

    private async Task TouchAssignmentAsync(FwAssignmentScopeType scopeType, string scopeCode, CancellationToken ct)
    {
        var record = await _db.FwAssignmentRecords
            .FirstOrDefaultAsync(x => x.ScopeType == scopeType && x.ScopeCode == scopeCode, ct);
        if (record == null)
        {
            record = new FwAssignmentRecord
            {
                ScopeType = scopeType,
                ScopeCode = scopeCode,
                AssignCount = 0,
                CreateDate = DateTime.UtcNow
            };
            _db.FwAssignmentRecords.Add(record);
        }

        record.LastAssignedAt = DateTime.UtcNow;
        record.AssignCount++;
        record.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static string AisleScopeCode(string layerCode, string aisleCode)
        => $"{layerCode}/{aisleCode}";
}
```

### FILE: Seven.Net8/Seven.Tests/Wcs/FourWayAllocatorTests.cs
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Packs.FourWay;

namespace Seven.Tests.Wcs;

public class FourWayAllocatorTests
{
    [Fact]
    public async Task SelectLayer_ShouldPrefer_HigherAllocationWeight()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedLayerPolicy(db, wh.Code, "Fw.L02", weight: 10);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 0);
        SeedAislePolicy(db, "Fw.L02", "Fw.A-L02", minEmpty: 0);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-L02", "Fw.N-L02-01");
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeTrue();
        result.LayerCode.Should().Be("Fw.L02");
        result.LocationCode.Should().Be("Fw.N-L02-01");
    }

    [Fact]
    public async Task SelectAisle_ShouldSkip_WhenMinEmptySlotsNotMet()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 2);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("宸?);
    }

    [Fact]
    public async Task SelectLocation_ShouldBook_IsBooked()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 0);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeTrue();
        result.LocationCode.Should().Be("Fw.N-L01-01");
        (await db.WmsLocations.SingleAsync(x => x.Code == "Fw.N-L01-01")).IsBooked.Should().BeTrue();
    }

    [Fact]
    public async Task SelectLayer_ShouldRotate_UsingFwAssignmentRecord()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 5);
        SeedLayerPolicy(db, wh.Code, "Fw.L02", weight: 5);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 0);
        SeedAislePolicy(db, "Fw.L02", "Fw.A-L02", minEmpty: 0);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-02");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-L02", "Fw.N-L02-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-L02", "Fw.N-L02-02");
        await db.SaveChangesAsync();

        var allocator = new FourWayInboundAllocator(db);
        var req = new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10);

        var first = await allocator.AllocateInboundAsync(req);
        var second = await allocator.AllocateInboundAsync(req);
        var third = await allocator.AllocateInboundAsync(req);

        first.Ok.Should().BeTrue();
        second.Ok.Should().BeTrue();
        third.Ok.Should().BeTrue();
        first.LayerCode.Should().BeOneOf("Fw.L01", "Fw.L02");
        second.LayerCode.Should().BeOneOf("Fw.L01", "Fw.L02");
        first.LayerCode.Should().NotBe(second.LayerCode);
        third.LayerCode.Should().Be(first.LayerCode);

        var records = await db.FwAssignmentRecords
            .Where(x => x.ScopeType == FwAssignmentScopeType.Layer)
            .ToListAsync();
        records.Should().HaveCount(2);
        records.Should().OnlyContain(x => x.AssignCount >= 1);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"FwAlloc_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static WmsWarehouse SeedWarehouse(SevenDbContext db)
    {
        var wh = new WmsWarehouse
        {
            Code = "WH-FW",
            Name = "鍥涘悜浠?,
            EnabledPackIds = WcsPackIds.FourWay,
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(wh);
        db.SaveChanges();
        return wh;
    }

    private static void SeedLayerPolicy(SevenDbContext db, string warehouseCode, string layerCode, int weight)
    {
        db.FwLayerPolicies.Add(new FwLayerPolicy
        {
            WarehouseCode = warehouseCode,
            ZoneCode = "Fw.Z",
            LayerCode = layerCode,
            MaxHeight = 9999,
            MaxWeight = 99999,
            IsAvailable = true,
            AllocationWeight = weight,
            CreateDate = DateTime.UtcNow
        });
    }

    private static void SeedAislePolicy(
        SevenDbContext db,
        string layerCode,
        string aisleCode,
        int minEmpty)
    {
        db.FwAislePolicies.Add(new FwAislePolicy
        {
            LayerCode = layerCode,
            AisleCode = aisleCode,
            MinEmptySlots = minEmpty,
            MaxShuttleCount = 0,
            DestinationPointCode = $"EP-{aisleCode}",
            AllocationWeight = 1,
            IsAvailable = true,
            MaxHeight = 9999,
            MaxWeight = 99999,
            CreateDate = DateTime.UtcNow
        });
    }

    private static void SeedLocation(
        SevenDbContext db,
        int warehouseId,
        string layerCode,
        string aisleCode,
        string locationCode)
    {
        var layer = db.WmsLayers.Local.FirstOrDefault(x => x.Code == layerCode)
                    ?? db.WmsLayers.FirstOrDefault(x => x.Code == layerCode);
        if (layer == null)
        {
            layer = new WmsLayer
            {
                WarehouseId = warehouseId,
                ZoneId = 0,
                PackId = WcsPackIds.FourWay,
                Code = layerCode,
                Name = layerCode,
                IsAvailable = true,
                CreateDate = DateTime.UtcNow
            };
            db.WmsLayers.Add(layer);
            db.SaveChanges();
        }

        var aisle = db.WmsAisles.Local.FirstOrDefault(x => x.Code == aisleCode)
                    ?? db.WmsAisles.FirstOrDefault(x => x.Code == aisleCode);
        if (aisle == null)
        {
            aisle = new WmsAisle
            {
                WarehouseId = warehouseId,
                ZoneId = 0,
                LayerId = layer.Id,
                PackId = WcsPackIds.FourWay,
                Code = aisleCode,
                Name = aisleCode,
                IsAvailable = true,
                CreateDate = DateTime.UtcNow
            };
            db.WmsAisles.Add(aisle);
            db.SaveChanges();
        }

        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = warehouseId,
            LayerId = layer.Id,
            AisleId = aisle.Id,
            PackId = WcsPackIds.FourWay,
            Code = locationCode,
            Aisle = aisleCode,
            CreateDate = DateTime.UtcNow
        });
    }
}
```
