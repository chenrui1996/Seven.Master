using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wcs;

public class OutboundToStackerE2ETests
{
    [Fact]
    public async Task OutboundApprove_Retrieval_ShouldMoveStockToDock()
    {
        var db = CreateDb();
        Seed(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var pack = new StackerWcsPack(db, control, port, new StackerPathDispatcher(db, port));
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        var dest = new StackerDestinationService(
            db, port, new StackerAisleAllocator(db), new StackerLocationAllocator(db),
            new StackerPathDispatcher(db, port), bus, pack);
        dest.Subscribe();

        await stock.ReceiveAsync(new ReceiveStockRequest("Stk.LOC-A1-01", "MAT-01", 10m, "TP-OUT"));
        var loc = await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-A1-01");
        loc.IsOccupied = true;
        loc.CurrentContainerCode = "TP-OUT";
        await db.SaveChangesAsync();

        var outbound = new OutboundOrderService(db, stock, new BusTransportOrderRequest(bus));
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-E2E-001",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, "Stk.LOC-A1-01", "Stk.DOCK-01", "TP-OUT")],
            WcsGroupNo: "G-OUT-1"));

        await outbound.ApproveAsync(order.Id);

        (await db.WmsStocks.SingleAsync(x => x.LocationCode == "Stk.LOC-A1-01")).AvailableQty.Should().Be(0m);
        (await db.StkRetrievalTasks.CountAsync()).Should().Be(1);
        (await db.StkPutAwayTasks.CountAsync()).Should().Be(0);

        var retrieval = await db.StkRetrievalTasks.SingleAsync();
        retrieval.Status.Should().Be(StkRetrievalStatus.Dispatched);
        retrieval.WcsGroupNo.Should().Be("G-OUT-1");
        retrieval.WcsPri.Should().Be(1);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "Stk.DOCK-01");

        var transport = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync();
        var leg = transport.Legs.Single();
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-OUT", "Stk.DOCK-01", "OK", leg.Id));

        (await db.WmsOutboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
        (await db.StkRetrievalTasks.SingleAsync()).Status.Should().Be(StkRetrievalStatus.Completed);
        (await db.WmsStocks.SingleAsync(x => x.Qty > 0)).LocationCode.Should().Be("Stk.DOCK-01");

        var src = await db.WmsLocations.SingleAsync(x => x.Code == "Stk.LOC-A1-01");
        src.IsOccupied.Should().BeFalse();
        var dock = await db.WmsLocations.SingleAsync(x => x.Code == "Stk.DOCK-01");
        dock.IsOccupied.Should().BeTrue();
        dock.CurrentContainerCode.Should().Be("TP-OUT");
    }

    [Fact]
    public async Task SameGroup_ShouldDispatchByWcsPriOrder()
    {
        var db = CreateDb();
        Seed(db);
        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = db.WmsWarehouses.Single().Id,
            PackId = "stacker",
            Code = "Stk.LOC-A1-02",
            Aisle = "A1"
        });
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var pack = new StackerWcsPack(db, control, port, new StackerPathDispatcher(db, port));
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        var dest = new StackerDestinationService(
            db, port, new StackerAisleAllocator(db), new StackerLocationAllocator(db),
            new StackerPathDispatcher(db, port), bus, pack);
        dest.Subscribe();

        await stock.ReceiveAsync(new ReceiveStockRequest("Stk.LOC-A1-01", "MAT-01", 5m, "TP-1"));
        await stock.ReceiveAsync(new ReceiveStockRequest("Stk.LOC-A1-02", "MAT-01", 5m, "TP-2"));

        var outbound = new OutboundOrderService(db, stock, new BusTransportOrderRequest(bus));
        // ???Line1 Pri=2?Line2 Pri=1 �???�?TP-2
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-GRP",
            WmsOrderType.Other,
            [
                new OutboundLineInput(1, "MAT-01", 5m, "Stk.LOC-A1-01", "Stk.DOCK-01", "TP-1", WcsPri: 2),
                new OutboundLineInput(2, "MAT-01", 5m, "Stk.LOC-A1-02", "Stk.DOCK-01", "TP-2", WcsPri: 1)
            ],
            WcsGroupNo: "G1"));

        await outbound.ApproveAsync(order.Id);

        var tasks = await db.StkRetrievalTasks.OrderBy(x => x.WcsPri).ToListAsync();
        tasks.Should().HaveCount(2);
        tasks[0].ContainerCode.Should().Be("TP-2");
        tasks[0].Status.Should().Be(StkRetrievalStatus.Dispatched);
        tasks[1].ContainerCode.Should().Be("TP-1");
        tasks[1].Status.Should().Be(StkRetrievalStatus.Suspended);

        port.DispatchedDestinations.Should().ContainSingle();
        port.DispatchedDestinations.Single().ContainerCode.Should().Be("TP-2");

        var leg2 = await db.BusTransportLegs.SingleAsync(x => x.ContainerCode == "TP-2");
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-2", "Stk.DOCK-01", "OK", leg2.Id));

        tasks = await db.StkRetrievalTasks.OrderBy(x => x.WcsPri).ToListAsync();
        tasks[0].Status.Should().Be(StkRetrievalStatus.Completed);
        tasks[1].Status.Should().Be(StkRetrievalStatus.Dispatched);
        port.DispatchedDestinations.Should().HaveCount(2);
        port.DispatchedDestinations.Last().ContainerCode.Should().Be("TP-1");

        var leg1 = await db.BusTransportLegs.SingleAsync(x => x.ContainerCode == "TP-1");
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-1", "Stk.DOCK-01", "OK", leg1.Id));

        (await db.WmsOutboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
        (await db.StkRetrievalTasks.CountAsync(x => x.Status == StkRetrievalStatus.Completed)).Should().Be(2);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"OutboundStkE2E_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void Seed(SevenDbContext db)
    {
        var warehouse = new WmsWarehouse { Code = "WH1", Name = "??", EnabledPackIds = "stacker" };
        db.WmsWarehouses.Add(warehouse);
        db.SaveChanges();
        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = warehouse.Id, PackId = "stacker", Code = "Stk.LOC-A1-01", Aisle = "A1" },
            new WmsLocation { WarehouseId = warehouse.Id, PackId = "stacker", Code = "Stk.DOCK-01", IsHandover = true });
    }
}
