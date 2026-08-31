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
        result.Message.Should().Contain("巷");
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
    public async Task SelectAisle_ShouldUseSelectedLayerOnly_WhenAisleCodeSharedAcrossLayers()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedLayerPolicy(db, wh.Code, "Fw.L02", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-SHARED", minEmpty: 2);
        SeedAislePolicy(db, "Fw.L02", "Fw.A-SHARED", minEmpty: 0);

        // L01 仅 1 空位，单独不满足 MinEmpty=2；L02 多空位不得串入 L01 计数
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-SHARED", "Fw.N-L01-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-SHARED", "Fw.N-L02-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-SHARED", "Fw.N-L02-02");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-SHARED", "Fw.N-L02-03");

        // L02 同 Code 巷道不可用：若未按 LayerId 过滤，ToDictionary 可能误伤 L01
        var l02 = await db.WmsLayers.SingleAsync(x => x.Code == "Fw.L02");
        var aisleL02 = await db.WmsAisles.SingleAsync(x => x.Code == "Fw.A-SHARED" && x.LayerId == l02.Id);
        aisleL02.IsAvailable = false;
        await db.SaveChangesAsync();

        var fail = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10, PreferredLayerCode: "Fw.L01"));
        fail.Ok.Should().BeFalse();
        fail.Message.Should().Contain("巷");

        // L01 补足空位后应成功；L02 不可用不得阻塞本层
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-SHARED", "Fw.N-L01-02");
        await db.SaveChangesAsync();

        var ok = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10, PreferredLayerCode: "Fw.L01"));
        ok.Ok.Should().BeTrue();
        ok.LayerCode.Should().Be("Fw.L01");
        ok.AisleCode.Should().Be("Fw.A-SHARED");
        ok.LocationCode.Should().BeOneOf("Fw.N-L01-01", "Fw.N-L01-02");
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
            Name = "四向仓",
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

        var aisle = db.WmsAisles.Local.FirstOrDefault(x => x.Code == aisleCode && x.LayerId == layer.Id)
                    ?? db.WmsAisles.FirstOrDefault(x => x.Code == aisleCode && x.LayerId == layer.Id);
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
