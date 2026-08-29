using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wcs;

public class StackerDoubleDeepTests
{
    [Fact]
    public async Task SelectLocation_ShouldSkipDeep_WhenShallowEmpty()
    {
        var db = CreateDb();
        SeedBinPair(db, shallowOccupied: false);
        await db.SaveChangesAsync();

        var code = await new StackerLocationAllocator(db).SelectLocationAsync("A1", book: false);
        code.Should().Be("Stk.LOC-S");
    }

    [Fact]
    public async Task SelectLocation_ShouldAllowDeep_WhenShallowOccupied()
    {
        var db = CreateDb();
        SeedBinPair(db, shallowOccupied: true);
        await db.SaveChangesAsync();

        var code = await new StackerLocationAllocator(db).SelectLocationAsync("A1", book: false);
        code.Should().Be("Stk.LOC-D");
    }

    [Fact]
    public async Task SelectLocation_ShouldBookSisters_InBinGroup()
    {
        var db = CreateDb();
        SeedBinPair(db, shallowOccupied: false);
        await db.SaveChangesAsync();

        var code = await new StackerLocationAllocator(db).SelectLocationAsync("A1", book: true);
        code.Should().Be("Stk.LOC-S");
        (await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-S")).IsBooked.Should().BeTrue();
        (await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-D")).IsBooked.Should().BeTrue();
    }

    [Fact]
    public async Task SelectLocation_ShouldSkip_InLockBin()
    {
        var db = CreateDb();
        SeedBinPair(db, shallowOccupied: false);
        await db.SaveChangesAsync();

        var profile = await db.StkLocationProfiles.SingleAsync(x => x.LocationCode == "Stk.LOC-S");
        profile.InLockBin = true;
        await db.SaveChangesAsync();

        var code = await new StackerLocationAllocator(db).SelectLocationAsync("A1", book: false);
        code.Should().BeNull();
    }

    [Fact]
    public async Task SelectAisle_ShouldSkip_WhenWarehouseCycleCountLocked()
    {
        var db = CreateDb();
        var wh = new WmsWarehouse
        {
            Code = "WH1",
            Name = "WH1",
            EnabledPackIds = "stacker",
            IsCycleCountLocked = true
        };
        db.WmsWarehouses.Add(wh);
        await db.SaveChangesAsync();

        db.WmsAisles.Add(new WmsAisle
        {
            WarehouseId = wh.Id,
            ZoneId = 1,
            PackId = "stacker",
            Code = "A1",
            Name = "A1",
            EpPointCode = "EP-A1",
            IsAvailable = true
        });
        db.StkAssignmentPolicies.Add(new StkAssignmentPolicy
        {
            AisleCode = "A1",
            IsAvailable = true,
            MaxHeight = 3,
            MaxWeight = 100,
            DestinationPointCode = "EP-A1"
        });
        await db.SaveChangesAsync();

        var result = await new StackerAisleAllocator(db).SelectAisleAsync(1, 10);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeepRetrieval_ShouldTransferShallowFirst_ThenDispatchDeep()
    {
        var db = CreateDb();
        var wh = new WmsWarehouse { Code = "WH1", Name = "WH1", EnabledPackIds = "stacker" };
        db.WmsWarehouses.Add(wh);
        await db.SaveChangesAsync();

        db.WmsLocations.AddRange(
            new WmsLocation
            {
                WarehouseId = wh.Id,
                PackId = "stacker",
                Code = "Stk.LOC-S",
                Aisle = "A1",
                Depth = "1",
                IsOccupied = true,
                CurrentContainerCode = "TP-SHALLOW"
            },
            new WmsLocation
            {
                WarehouseId = wh.Id,
                PackId = "stacker",
                Code = "Stk.LOC-D",
                Aisle = "A1",
                Depth = "2",
                IsOccupied = true,
                CurrentContainerCode = "TP-DEEP"
            },
            new WmsLocation
            {
                WarehouseId = wh.Id,
                PackId = "stacker",
                Code = "Stk.LOC-FREE",
                Aisle = "A1",
                Depth = "1"
            },
            new WmsLocation
            {
                WarehouseId = wh.Id,
                PackId = "stacker",
                Code = "Stk.DOCK-01",
                Aisle = "A1"
            });
        db.StkLocationProfiles.AddRange(
            new StkLocationProfile { LocationCode = "Stk.LOC-S", BinGroupCode = "BG1" },
            new StkLocationProfile { LocationCode = "Stk.LOC-D", BinGroupCode = "BG1" });
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var path = new StackerPathDispatcher(db, port);
        var completion = new WmsTransportCompletionHandler(db, stock);

        await stock.ReceiveAsync(new ReceiveStockRequest("Stk.LOC-S", "MAT-01", 1m, "TP-SHALLOW"));
        await stock.ReceiveAsync(new ReceiveStockRequest("Stk.LOC-D", "MAT-01", 1m, "TP-DEEP"));

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(port);
        services.AddSingleton(control);
        services.AddSingleton(path);
        services.AddSingleton(completion);
        services.AddSingleton(sp => new StackerDepthGuard(db, sp));
        services.AddSingleton(sp => new StackerWcsPack(
            db, control, port, path, sp.GetRequiredService<StackerDepthGuard>()));
        services.AddSingleton(sp => new OrchestrationBus(
            db,
            new WcsPackResolver([sp.GetRequiredService<StackerWcsPack>()]),
            completion));
        services.AddSingleton<IOrchestrationBus>(sp => sp.GetRequiredService<OrchestrationBus>());
        var root = services.BuildServiceProvider();

        var pack = root.GetRequiredService<StackerWcsPack>();
        var bus = root.GetRequiredService<IOrchestrationBus>();
        var dest = new StackerDestinationService(
            db, port, new StackerAisleAllocator(db), new StackerLocationAllocator(db),
            path, bus, pack);
        dest.Subscribe();

        var deepOrderId = await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-DEEP", "Stk.LOC-D", "Stk.DOCK-01",
            RefType: "OutboundOrder",
            RefId: "OUT-DEEP",
            WcsGroupNo: "G-DEEP",
            WcsPri: 1));

        var deep = await db.StkRetrievalTasks.SingleAsync(x => x.ContainerCode == "TP-DEEP");
        deep.Status.Should().Be(StkRetrievalStatus.Suspended);
        deep.WcsPri.Should().Be(2);

        var transfer = await db.StkRetrievalTasks.SingleAsync(x => x.ContainerCode == "TP-SHALLOW");
        transfer.Status.Should().Be(StkRetrievalStatus.Dispatched);
        transfer.WcsPri.Should().Be(1);

        var transferOrder = await db.BusTransportOrders
            .Include(x => x.Legs)
            .SingleAsync(x => x.RefType == "StackerTransfer");
        var transferLeg = transferOrder.Legs.Single();
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-SHALLOW", transferOrder.ToLocationCode, "OK", transferLeg.Id));

        (await db.StkRetrievalTasks.SingleAsync(x => x.ContainerCode == "TP-SHALLOW"))
            .Status.Should().Be(StkRetrievalStatus.Completed);
        (await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-S")).IsOccupied.Should().BeFalse();

        deep = await db.StkRetrievalTasks.SingleAsync(x => x.ContainerCode == "TP-DEEP");
        deep.Status.Should().Be(StkRetrievalStatus.Dispatched);

        var deepOrder = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync(x => x.Id == deepOrderId);
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-DEEP", "Stk.DOCK-01", "OK", deepOrder.Legs.Single().Id));

        (await db.StkRetrievalTasks.SingleAsync(x => x.ContainerCode == "TP-DEEP"))
            .Status.Should().Be(StkRetrievalStatus.Completed);
        (await db.WmsStocks.SingleAsync(x => x.ContainerCode == "TP-DEEP" && x.Qty > 0))
            .LocationCode.Should().Be("Stk.DOCK-01");
    }

    private static void SeedBinPair(SevenDbContext db, bool shallowOccupied)
    {
        db.WmsLocations.AddRange(
            new WmsLocation
            {
                WarehouseId = 1,
                PackId = "stacker",
                Code = "Stk.LOC-S",
                Aisle = "A1",
                Depth = "1",
                Layer = "1",
                Column = "1",
                Row = "1",
                IsOccupied = shallowOccupied,
                CurrentContainerCode = shallowOccupied ? "TP-S" : null
            },
            new WmsLocation
            {
                WarehouseId = 1,
                PackId = "stacker",
                Code = "Stk.LOC-D",
                Aisle = "A1",
                Depth = "2",
                Layer = "1",
                Column = "1",
                Row = "1"
            });
        db.StkLocationProfiles.AddRange(
            new StkLocationProfile { LocationCode = "Stk.LOC-S", BinGroupCode = "BG1" },
            new StkLocationProfile { LocationCode = "Stk.LOC-D", BinGroupCode = "BG1" });
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"StkDD_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
