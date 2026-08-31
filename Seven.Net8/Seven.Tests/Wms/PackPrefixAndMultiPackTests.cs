using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wms;

public class PackPrefixAndMultiPackTests
{
    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"PackPrefix_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Theory]
    [InlineData("BIN-01", "stacker", "Stk.BIN-01")]
    [InlineData("Stk.BIN-01", "stacker", "Stk.BIN-01")]
    [InlineData("Fw.N-1", "stacker", "Stk.N-1")]
    [InlineData("N-1205", "fourway", "Fw.N-1205")]
    public void EnsurePrefix_ShouldNormalize(string input, string packId, string expected) =>
        PackCodeRules.EnsurePrefix(input, packId).Should().Be(expected);

    [Fact]
    public void HasValidPrefix_ShouldRejectWrongPack()
    {
        PackCodeRules.HasValidPrefix("Stk.BIN-01", WcsPackIds.FourWay).Should().BeFalse();
        PackCodeRules.HasValidPrefix("BIN-01", WcsPackIds.Stacker).Should().BeFalse();
        PackCodeRules.HasValidPrefix("Fw.N-1", WcsPackIds.FourWay).Should().BeTrue();
    }

    [Fact]
    public void WarehousePackRules_ShouldRequireAtLeastOne()
    {
        var act = () => WarehousePackRules.EnsureAtLeastOne("");
        act.Should().Throw<WmsDomainException>();
        WarehousePackRules.EnsureContains(null, WcsPackIds.Stacker).Should().Be("stacker");
        WarehousePackRules.EnsureContains("stacker", WcsPackIds.FourWay)
            .Should().Be("stacker,fourway");
    }

    [Fact]
    public async Task LocationService_ShouldReject_WrongPrefixForPack()
    {
        var db = CreateDb();
        db.WmsWarehouses.Add(new WmsWarehouse
        {
            Code = "WH1",
            Name = "主仓",
            EnabledPackIds = "stacker",
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var whId = db.WmsWarehouses.Single().Id;
        var svc = new LocationService(db);

        var result = await svc.AddAsync(new WmsLocation
        {
            WarehouseId = whId,
            PackId = WcsPackIds.Stacker,
            Code = "Fw.BIN-01"
        });

        result.Status.Should().BeFalse();
        result.Message.Should().Contain("前缀");
        (await db.WmsLocations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LocationService_ShouldReject_PackNotEnabledOnWarehouse()
    {
        var db = CreateDb();
        db.WmsWarehouses.Add(new WmsWarehouse
        {
            Code = "WH1",
            Name = "主仓",
            EnabledPackIds = "stacker",
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var whId = db.WmsWarehouses.Single().Id;
        var svc = new LocationService(db);

        var result = await svc.AddAsync(new WmsLocation
        {
            WarehouseId = whId,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.N-1"
        });

        result.Status.Should().BeFalse();
        result.Message.Should().Contain("未启用");
    }

    [Fact]
    public async Task SameWarehouse_ShouldHost_StackerAndFourWayLocations()
    {
        var db = CreateDb();
        var warehouse = new WmsWarehouse
        {
            Code = "WH-MIX",
            Name = "混合仓",
            EnabledPackIds = "stacker,fourway",
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var zoneFw = new WmsZone
        {
            WarehouseId = warehouse.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.Z-ASRS",
            Name = "四向区",
            CreateDate = DateTime.UtcNow
        };
        db.WmsZones.Add(zoneFw);
        await db.SaveChangesAsync();

        var layer = new WmsLayer
        {
            WarehouseId = warehouse.Id,
            ZoneId = zoneFw.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.L02",
            Name = "二层",
            CreateDate = DateTime.UtcNow
        };
        db.WmsLayers.Add(layer);
        await db.SaveChangesAsync();

        var aisleFw = new WmsAisle
        {
            WarehouseId = warehouse.Id,
            ZoneId = zoneFw.Id,
            LayerId = layer.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.A-L02-01",
            Name = "二层巷1",
            CreateDate = DateTime.UtcNow
        };
        db.WmsAisles.Add(aisleFw);
        await db.SaveChangesAsync();

        var svc = new LocationService(db);
        var stk = await svc.AddAsync(new WmsLocation
        {
            WarehouseId = warehouse.Id,
            PackId = WcsPackIds.Stacker,
            Code = "B-01-02-03-1",
            Aisle = "Stk.A-01"
        });
        var fw = await svc.AddAsync(new WmsLocation
        {
            WarehouseId = warehouse.Id,
            PackId = WcsPackIds.FourWay,
            Code = "N-1205",
            LayerId = layer.Id,
            AisleId = aisleFw.Id,
            Aisle = aisleFw.Code
        });

        stk.Status.Should().BeTrue();
        fw.Status.Should().BeTrue();

        var codes = await db.WmsLocations.Select(x => x.Code).OrderBy(x => x).ToListAsync();
        codes.Should().Equal("Fw.N-1205", "Stk.B-01-02-03-1");
        (await db.WmsLayers.CountAsync()).Should().Be(1);

        var resolver = new WcsLocationAllocatorResolver(
            [new StackerLocationSchema(), new FourWayLocationSchema()],
            [
                new StackerInboundAllocator(new StackerAisleAllocator(db), new StackerLocationAllocator(db)),
                new FourWayInboundAllocator(db)
            ]);
        resolver.GetSchema(WcsPackIds.Stacker).HierarchyLevels.Should().Equal("Zone", "Aisle", "Location");
        resolver.GetSchema(WcsPackIds.FourWay).HierarchyLevels.Should().Equal("Zone", "Layer", "Aisle", "Location");

        var alloc = await resolver.GetAllocator(WcsPackIds.FourWay).AllocateInboundAsync(
            new AllocationRequest(warehouse.Id, WcsPackIds.FourWay, PreferredLayerCode: "Fw.L02"));
        alloc.Ok.Should().BeTrue();
        alloc.LocationCode.Should().Be("Fw.N-1205");
        alloc.LayerCode.Should().Be("Fw.L02");
    }
}
