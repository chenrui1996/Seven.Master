using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wcs;

public class InboundToStackerE2ETests
{
    [Fact]
    public async Task BuildPallet_Allocate_ThenStackerSimulate_ShouldPutStockAtStkLocation()
    {
        var db = CreateDb();
        SeedWarehouseAndLocations(db);
        SeedStackerMaster(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var pack = new StackerWcsPack(db, control);
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        var dest = new StackerDestinationService(
            db,
            port,
            new StackerAisleAllocator(db),
            new StackerLocationAllocator(db),
            new StackerPathDispatcher(db, port),
            bus);
        dest.Subscribe();

        var resolver = new WcsLocationAllocatorResolver(
            [new StackerLocationSchema()],
            [new StackerInboundAllocator(new StackerAisleAllocator(db), new StackerLocationAllocator(db))]);
        var inbound = new InboundOrderService(db, stock, new BusTransportOrderRequest(bus), resolver);

        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-E2E-001",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-01", 10m, FromLocation: "Stk.RECV-01")]));

        await inbound.ApproveAsync(order.Id);
        var detail = await inbound.BuildPalletAsync(order.Id, new BuildPalletRequest(
            LineNo: 1,
            Qty: 10m,
            ContainerCode: "TP-E2E",
            ReceiveLocationCode: "Stk.RECV-01",
            Height: 1,
            Weight: 10));

        detail.TargetLocationCode.Should().Be("Stk.LOC-A1-01");
        detail.AssignedAisle.Should().Be("A1");
        detail.Status.Should().Be(WmsInboundDetailStatus.Transporting);
        detail.PackId.Should().Be(WcsPackIds.Stacker);

        var booked = await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-A1-01");
        booked.IsBooked.Should().BeTrue();

        var afterReceive = await db.WmsInboundOrders.Include(x => x.Lines).SingleAsync();
        afterReceive.Status.Should().Be(WmsOrderStatus.Executing);
        var recvStock = await db.WmsStocks.SingleAsync(x => x.Qty > 0);
        recvStock.LocationCode.Should().Be("Stk.RECV-01");
        recvStock.Qty.Should().Be(10m);

        var transport = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync();
        transport.FromLocationCode.Should().Be("Stk.RECV-01");
        transport.ToLocationCode.Should().Be("Stk.LOC-A1-01");
        transport.RefType.Should().Be("InboundDetail");
        transport.RefId.Should().Be(detail.Id.ToString());
        var leg = transport.Legs.Single();

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-E2E", "RP_IN_01", 1, 10, "OK"));
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-E2E", "EP-A1", "OK", leg.Id));

        var afterDone = await db.WmsInboundOrders.AsNoTracking().SingleAsync();
        afterDone.Status.Should().Be(WmsOrderStatus.Completed);

        var target = await db.WmsStocks.SingleAsync(x => x.Qty > 0);
        target.LocationCode.Should().Be("Stk.LOC-A1-01");
        target.Qty.Should().Be(10m);
        target.ContainerCode.Should().Be("TP-E2E");

        var putaway = await db.StkPutAwayTasks.SingleAsync();
        putaway.Status.Should().Be(StkPutAwayStatus.Completed);

        var finishedDetail = await db.WmsInboundDetails.SingleAsync();
        finishedDetail.Status.Should().Be(WmsInboundDetailStatus.Completed);

        var loc = await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-A1-01");
        loc.IsBooked.Should().BeFalse();
        loc.IsOccupied.Should().BeTrue();
        loc.CurrentContainerCode.Should().Be("TP-E2E");
    }

    [Fact]
    public async Task BuildPallet_PartialQty_ShouldAllowMultipleDetails()
    {
        var db = CreateDb();
        SeedWarehouseAndLocations(db);
        SeedStackerMaster(db);
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        var resolver = new WcsLocationAllocatorResolver(
            [new StackerLocationSchema()],
            [new StackerInboundAllocator(new StackerAisleAllocator(db), new StackerLocationAllocator(db))]);
        // 无运输：同位组盘
        var inbound = new InboundOrderService(db, stock, transport: null, resolver);

        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-PART",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-01", 10m, FromLocation: "Stk.RECV-01", ToLocation: "Stk.RECV-01")]));
        await inbound.ApproveAsync(order.Id);

        await inbound.BuildPalletAsync(order.Id, new BuildPalletRequest(1, 4m, "TP-A", "Stk.RECV-01", "Stk.RECV-01", AllocateTarget: false));
        await inbound.BuildPalletAsync(order.Id, new BuildPalletRequest(1, 6m, "TP-B", "Stk.RECV-01", "Stk.RECV-01", AllocateTarget: false));

        (await db.WmsInboundDetails.CountAsync()).Should().Be(2);
        (await db.WmsInboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
        (await db.WmsStocks.SumAsync(x => x.Qty)).Should().Be(10m);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"InboundStkE2E_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void SeedWarehouseAndLocations(SevenDbContext db)
    {
        var warehouse = new WmsWarehouse { Code = "WH1", Name = "主仓", EnabledPackIds = "stacker" };
        db.WmsWarehouses.Add(warehouse);
        db.SaveChanges();
        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = warehouse.Id, PackId = "stacker", Code = "Stk.RECV-01" },
            new WmsLocation { WarehouseId = warehouse.Id, PackId = "stacker", Code = "Stk.LOC-A1-01", Aisle = "A1" });
    }

    private static void SeedStackerMaster(SevenDbContext db)
    {
        db.StkAssignmentPolicies.Add(new StkAssignmentPolicy
        {
            AisleCode = "A1",
            IsAvailable = true,
            MaxHeight = 3,
            MaxWeight = 100,
            DestinationPointCode = "EP-A1"
        });
        db.StkRequestPoints.Add(new StkRequestPoint
        {
            Code = "RP_IN_01",
            PointType = StkRequestPointType.AisleRequest,
            IsEnabled = true
        });
    }
}
