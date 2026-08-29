using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wcs;

public class OutboundToFourWayE2ETests
{
    [Fact]
    public async Task OutboundApprove_Retrieval_ShouldMoveStockToDock()
    {
        var db = CreateDb();
        Seed(db);
        await db.SaveChangesAsync();

        var (bus, port, pack, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-A1-01", "MAT-01", 10m, "TP-OUT"));
        var loc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-A1-01");
        loc.IsOccupied = true;
        loc.CurrentContainerCode = "TP-OUT";
        await db.SaveChangesAsync();

        var outbound = new OutboundOrderService(db, stock, new BusTransportOrderRequest(bus));
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-FW-E2E-001",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, "Fw.LOC-A1-01", "Fw.DOCK-01", "TP-OUT")],
            WcsGroupNo: "G-FW-OUT-1"));

        await outbound.ApproveAsync(order.Id);

        (await db.WmsStocks.SingleAsync(x => x.LocationCode == "Fw.LOC-A1-01")).AvailableQty.Should().Be(0m);
        (await db.FwRetrievalTasks.CountAsync()).Should().Be(1);
        (await db.FwPutAwayTasks.CountAsync()).Should().Be(0);

        var retrieval = await db.FwRetrievalTasks.SingleAsync();
        retrieval.Status.Should().Be(FwRetrievalStatus.Dispatched);
        retrieval.WcsGroupNo.Should().Be("G-FW-OUT-1");
        retrieval.WcsPri.Should().Be(1);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "Fw.DOCK-01");

        var parking = await db.FwParkingLedgers.SingleAsync();
        parking.Status.Should().Be(FwParkingStatus.Occupied);
        parking.OwnerId.Should().Be(retrieval.Id);
        parking.ContainerCode.Should().Be("TP-OUT");

        var transport = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync();
        var leg = transport.Legs.Single();
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-OUT", "Fw.DOCK-01", "OK", leg.Id));

        (await db.WmsOutboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
        (await db.FwRetrievalTasks.SingleAsync()).Status.Should().Be(FwRetrievalStatus.Completed);
        (await db.WmsStocks.SingleAsync(x => x.Qty > 0)).LocationCode.Should().Be("Fw.DOCK-01");

        parking = await db.FwParkingLedgers.SingleAsync();
        parking.Status.Should().Be(FwParkingStatus.Free);
        parking.OwnerId.Should().BeNull();
        parking.ContainerCode.Should().BeNull();

        var src = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-A1-01");
        src.IsOccupied.Should().BeFalse();
        var dock = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.DOCK-01");
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
            PackId = WcsPackIds.FourWay,
            Code = "Fw.LOC-A1-02",
            Aisle = "Fw.A1",
            CreateDate = DateTime.UtcNow
        });
        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-02",
            LocationCode = "Fw.PARK-02",
            Status = FwParkingStatus.Free,
            ShuttleNo = "S02",
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (bus, port, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-A1-01", "MAT-01", 5m, "TP-1"));
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-A1-02", "MAT-01", 5m, "TP-2"));

        var outbound = new OutboundOrderService(db, stock, new BusTransportOrderRequest(bus));
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-FW-GRP",
            WmsOrderType.Other,
            [
                new OutboundLineInput(1, "MAT-01", 5m, "Fw.LOC-A1-01", "Fw.DOCK-01", "TP-1", WcsPri: 2),
                new OutboundLineInput(2, "MAT-01", 5m, "Fw.LOC-A1-02", "Fw.DOCK-01", "TP-2", WcsPri: 1)
            ],
            WcsGroupNo: "G-FW-1"));

        await outbound.ApproveAsync(order.Id);

        var tasks = await db.FwRetrievalTasks.OrderBy(x => x.WcsPri).ToListAsync();
        tasks.Should().HaveCount(2);
        tasks[0].ContainerCode.Should().Be("TP-2");
        tasks[0].Status.Should().Be(FwRetrievalStatus.Dispatched);
        tasks[1].ContainerCode.Should().Be("TP-1");
        tasks[1].Status.Should().Be(FwRetrievalStatus.Suspended);

        port.DispatchedDestinations.Should().ContainSingle();
        port.DispatchedDestinations.Single().ContainerCode.Should().Be("TP-2");

        (await db.FwParkingLedgers.CountAsync(x => x.Status == FwParkingStatus.Occupied)).Should().Be(1);

        var leg2 = await db.BusTransportLegs.SingleAsync(x => x.ContainerCode == "TP-2");
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-2", "Fw.DOCK-01", "OK", leg2.Id));

        tasks = await db.FwRetrievalTasks.OrderBy(x => x.WcsPri).ToListAsync();
        tasks[0].Status.Should().Be(FwRetrievalStatus.Completed);
        tasks[1].Status.Should().Be(FwRetrievalStatus.Dispatched);
        port.DispatchedDestinations.Should().HaveCount(2);
        port.DispatchedDestinations.Last().ContainerCode.Should().Be("TP-1");

        var leg1 = await db.BusTransportLegs.SingleAsync(x => x.ContainerCode == "TP-1");
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-1", "Fw.DOCK-01", "OK", leg1.Id));

        (await db.WmsOutboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
        (await db.FwRetrievalTasks.CountAsync(x => x.Status == FwRetrievalStatus.Completed)).Should().Be(2);
        (await db.FwParkingLedgers.CountAsync(x => x.Status == FwParkingStatus.Free)).Should().Be(2);
    }

    [Fact]
    public async Task NoFreeParking_ShouldSuspendRetrieval()
    {
        var db = CreateDb();
        Seed(db, seedParking: false);
        await db.SaveChangesAsync();

        var (bus, port, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-A1-01", "MAT-01", 10m, "TP-NP"));
        var loc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-A1-01");
        loc.IsOccupied = true;
        loc.CurrentContainerCode = "TP-NP";
        await db.SaveChangesAsync();

        var outbound = new OutboundOrderService(db, stock, new BusTransportOrderRequest(bus));
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-FW-NP",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, "Fw.LOC-A1-01", "Fw.DOCK-01", "TP-NP")],
            WcsGroupNo: "G-NP"));

        await outbound.ApproveAsync(order.Id);

        var retrieval = await db.FwRetrievalTasks.SingleAsync();
        retrieval.Status.Should().Be(FwRetrievalStatus.Suspended);
        port.DispatchedDestinations.Should().BeEmpty();
    }

    private static (OrchestrationBus Bus, InMemoryEquipmentTriggerPort Port, FourWayWcsPack Pack, FourWayDestinationService Dest)
        CreateStack(SevenDbContext db)
    {
        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var path = new FourWayPathDispatcher(db, port, new FourWayTrafficGuard());
        var pack = new FourWayWcsPack(db, control, port, path);
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        var dest = new FourWayDestinationService(
            db, port, new FourWayInboundAllocator(db), path, bus, pack);
        return (bus, port, pack, dest);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"OutboundFwE2E_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void Seed(SevenDbContext db, bool seedParking = true)
    {
        var warehouse = new WmsWarehouse
        {
            Code = "WH-FW-OUT",
            Name = "四向出库仓",
            EnabledPackIds = WcsPackIds.FourWay,
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(warehouse);
        db.SaveChanges();

        db.WmsLocations.AddRange(
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.LOC-A1-01",
                Aisle = "Fw.A1",
                CreateDate = DateTime.UtcNow
            },
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.DOCK-01",
                IsHandover = true,
                CreateDate = DateTime.UtcNow
            });

        if (seedParking)
        {
            db.FwParkingLedgers.Add(new FwParkingLedger
            {
                Code = "PK-01",
                LocationCode = "Fw.PARK-01",
                Status = FwParkingStatus.Free,
                ShuttleNo = "S01",
                CreateDate = DateTime.UtcNow
            });
        }
    }
}
