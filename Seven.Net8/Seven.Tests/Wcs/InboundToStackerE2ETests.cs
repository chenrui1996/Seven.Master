using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

public class InboundToStackerE2ETests
{
    [Fact]
    public async Task InboundReceive_ThenStackerSimulate_ShouldPutStockAtTarget()
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
            bus);
        dest.Subscribe();

        var inbound = new InboundOrderService(db, stock, new BusTransportOrderRequest(bus));
        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-E2E-001",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-01", 10m, ContainerCode: "TP-E2E", FromLocation: "RECV-01", ToLocation: "LOC-A1-01")]));

        await inbound.ApproveAsync(order.Id);
        await inbound.ReceiveAndBuildPalletAsync(order.Id);

        var afterReceive = await db.WmsInboundOrders.Include(x => x.Lines).SingleAsync();
        afterReceive.Status.Should().Be(WmsOrderStatus.Executing);
        var recvStock = await db.WmsStocks.SingleAsync(x => x.Qty > 0);
        recvStock.LocationCode.Should().Be("RECV-01");
        recvStock.Qty.Should().Be(10m);

        var transport = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync();
        transport.FromLocationCode.Should().Be("RECV-01");
        transport.ToLocationCode.Should().Be("LOC-A1-01");
        transport.RefType.Should().Be("InboundOrder");
        transport.RefId.Should().Be("IN-E2E-001");
        var leg = transport.Legs.Single();

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-E2E", "RP_IN_01", 1, 10, "OK"));
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-E2E", "EP-A1", "OK", leg.Id));

        var afterDone = await db.WmsInboundOrders.AsNoTracking().SingleAsync();
        afterDone.Status.Should().Be(WmsOrderStatus.Completed);

        var target = await db.WmsStocks.SingleAsync(x => x.Qty > 0);
        target.LocationCode.Should().Be("LOC-A1-01");
        target.Qty.Should().Be(10m);
        target.ContainerCode.Should().Be("TP-E2E");

        var putaway = await db.StkPutAwayTasks.SingleAsync();
        putaway.Status.Should().Be(StkPutAwayStatus.Completed);
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
        var warehouse = new WmsWarehouse { Code = "WH1", Name = "主仓" };
        db.WmsWarehouses.Add(warehouse);
        db.SaveChanges();
        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = warehouse.Id, Code = "RECV-01" },
            new WmsLocation { WarehouseId = warehouse.Id, Code = "LOC-A1-01", Aisle = "A1" });
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
