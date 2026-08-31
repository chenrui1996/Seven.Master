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

public class FourWayHoistE2ETests
{
    [Fact]
    public async Task CrossLayer_Transfer_ShouldMoveStockViaHoistStages()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        await db.SaveChangesAsync();

        var (bus, port, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-L1-01", "MAT-01", 10m, "TP-XL"));
        var srcLoc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-L1-01");
        srcLoc.IsOccupied = true;
        srcLoc.CurrentContainerCode = "TP-XL";
        await db.SaveChangesAsync();

        var orderId = await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-XL",
            "Fw.LOC-L1-01",
            "Fw.LOC-L2-01",
            RefType: "FourWayTransfer",
            WcsPri: 1));

        var hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Status.Should().Be(FwHoistTaskStatus.Running);
        hoist.Stage.Should().Be(FwHoistStage.ToSrcAp);
        hoist.SrcAddress.Should().Be("Fw.H1-L1-AP");
        hoist.DesAddress.Should().Be("Fw.H1-L2-EP");

        var exec = await db.FwHoistExecTasks.SingleAsync();
        exec.Status.Should().Be(FwHoistExecStatus.Queued);

        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "Fw.H1-L1-AP");

        var leg = await db.BusTransportLegs.SingleAsync(x => x.OrderId == orderId);
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-XL", "Fw.H1-L1-AP", "OK", leg.Id));

        hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Stage.Should().Be(FwHoistStage.HoistLift);
        exec = await db.FwHoistExecTasks.SingleAsync();
        exec.Status.Should().Be(FwHoistExecStatus.Dispatched);
        port.DispatchedDestinations.Should().Contain(x => x.DestinationPointCode == "Fw.H1-L2-EP");

        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-XL", "Fw.H1-L2-EP", "OK", leg.Id));

        hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Stage.Should().Be(FwHoistStage.FromDesEp);
        exec = await db.FwHoistExecTasks.SingleAsync();
        exec.Status.Should().Be(FwHoistExecStatus.Completed);
        port.DispatchedDestinations.Should().Contain(x => x.DestinationPointCode == "Fw.LOC-L2-01");

        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-XL", "Fw.LOC-L2-01", "OK", leg.Id));

        hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Status.Should().Be(FwHoistTaskStatus.Completed);
        hoist.Stage.Should().Be(FwHoistStage.Done);
        (await db.BusTransportOrders.SingleAsync()).Status.Should().Be(BusOrderStatus.Completed);
        (await db.WmsStocks.SingleAsync(x => x.Qty > 0)).LocationCode.Should().Be("Fw.LOC-L2-01");

        var destLoc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-L2-01");
        destLoc.IsOccupied.Should().BeTrue();
        destLoc.CurrentContainerCode.Should().Be("TP-XL");
        (await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-L1-01")).IsOccupied.Should().BeFalse();
    }

    [Fact]
    public async Task SamePort_SecondExec_ShouldSuspendUntilFirstCompletes()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        // 第二组货位，仍走同一提升口
        var wh = db.WmsWarehouses.Single();
        var l1 = db.WmsLayers.Single(x => x.Code == "Fw.L01");
        var l2 = db.WmsLayers.Single(x => x.Code == "Fw.L02");
        db.WmsLocations.AddRange(
            new WmsLocation
            {
                WarehouseId = wh.Id,
                LayerId = l1.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.LOC-L1-02",
                CreateDate = DateTime.UtcNow
            },
            new WmsLocation
            {
                WarehouseId = wh.Id,
                LayerId = l2.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.LOC-L2-02",
                CreateDate = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var (bus, port, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-L1-01", "MAT-01", 5m, "TP-A"));
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-L1-02", "MAT-01", 5m, "TP-B"));
        await db.SaveChangesAsync();

        var orderA = await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-A", "Fw.LOC-L1-01", "Fw.LOC-L2-01", RefType: "FourWayTransfer", WcsPri: 1));
        var orderB = await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-B", "Fw.LOC-L1-02", "Fw.LOC-L2-02", RefType: "FourWayTransfer", WcsPri: 2));

        var legA = await db.BusTransportLegs.SingleAsync(x => x.OrderId == orderA);
        var legB = await db.BusTransportLegs.SingleAsync(x => x.OrderId == orderB);

        // 两车都到源层 AP
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback("TP-A", "Fw.H1-L1-AP", "OK", legA.Id));
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback("TP-B", "Fw.H1-L1-AP", "OK", legB.Id));

        var execs = await db.FwHoistExecTasks.OrderBy(x => x.WcsPri).ToListAsync();
        execs.Should().HaveCount(2);
        execs[0].ContainerCode.Should().Be("TP-A");
        execs[0].Status.Should().Be(FwHoistExecStatus.Dispatched);
        execs[1].ContainerCode.Should().Be("TP-B");
        execs[1].Status.Should().Be(FwHoistExecStatus.Suspended);

        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback("TP-A", "Fw.H1-L2-EP", "OK", legA.Id));

        execs = await db.FwHoistExecTasks.OrderBy(x => x.WcsPri).ToListAsync();
        execs[0].Status.Should().Be(FwHoistExecStatus.Completed);
        execs[1].Status.Should().Be(FwHoistExecStatus.Dispatched);
    }

    [Fact]
    public async Task ScanAndWake_SuspendedExec_ShouldDispatch()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var path = new FourWayPathDispatcher(db, port, new FourWayTrafficGuard());
        var hoist = new FourWayHoistOrchestrator(db, port, path);

        var taskId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwHoistTasks.Add(new FwHoistTask
        {
            Id = taskId,
            LegId = legId,
            ContainerCode = "TP-SCAN",
            HoistNo = "H1",
            SrcLayer = "Fw.L01",
            SrcAddress = "Fw.H1-L1-AP",
            DesLayer = "Fw.L02",
            DesAddress = "Fw.H1-L2-EP",
            FromCode = "Fw.LOC-L1-01",
            ToCode = "Fw.LOC-L2-01",
            Status = FwHoistTaskStatus.Running,
            Stage = FwHoistStage.HoistLift,
            WcsPri = 1,
            CreateDate = DateTime.UtcNow
        });
        db.FwHoistExecTasks.Add(new FwHoistExecTask
        {
            Id = Guid.NewGuid(),
            HoistTaskId = taskId,
            LegId = legId,
            ContainerCode = "TP-SCAN",
            HoistNo = "H1",
            SrcLayer = "Fw.L01",
            SrcAddress = "Fw.H1-L1-AP",
            DesLayer = "Fw.L02",
            DesAddress = "Fw.H1-L2-EP",
            Status = FwHoistExecStatus.Suspended,
            WcsPri = 1,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var woken = await hoist.ScanAndWakeQueuedExecsAsync();
        woken.Should().Be(1);

        var exec = await db.FwHoistExecTasks.SingleAsync();
        exec.Status.Should().Be(FwHoistExecStatus.Dispatched);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "Fw.H1-L2-EP");
    }

    [Fact]
    public async Task OutboundOrder_ShouldUseOutboundApEp()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        await db.SaveChangesAsync();

        var (bus, _, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-L1-01", "MAT-01", 10m, "TP-OB"));
        var srcLoc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-L1-01");
        srcLoc.IsOccupied = true;
        srcLoc.CurrentContainerCode = "TP-OB";
        await db.SaveChangesAsync();

        await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-OB",
            "Fw.LOC-L1-01",
            "Fw.LOC-L2-01",
            RefType: "OutboundOrder",
            WcsPri: 1));

        var hoist = await db.FwHoistTasks.SingleAsync();
        hoist.SrcAddress.Should().Be("Fw.H1-L1-OAP");
        hoist.DesAddress.Should().Be("Fw.H1-L2-OEP");
    }

    [Fact]
    public async Task BadFeedback_ShouldNotAdvanceHoistStage()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        await db.SaveChangesAsync();

        var (bus, port, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-L1-01", "MAT-01", 10m, "TP-BAD"));
        var srcLoc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-L1-01");
        srcLoc.IsOccupied = true;
        srcLoc.CurrentContainerCode = "TP-BAD";
        await db.SaveChangesAsync();

        var orderId = await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-BAD",
            "Fw.LOC-L1-01",
            "Fw.LOC-L2-01",
            RefType: "FourWayTransfer",
            WcsPri: 1));

        var leg = await db.BusTransportLegs.SingleAsync(x => x.OrderId == orderId);
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-BAD", "Fw.H1-L1-AP", "NG", leg.Id));

        var hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Stage.Should().Be(FwHoistStage.ToSrcAp);
        hoist.Status.Should().Be(FwHoistTaskStatus.Running);

        var exec = await db.FwHoistExecTasks.SingleAsync();
        exec.Status.Should().Be(FwHoistExecStatus.Queued);

        // OK 后再到 AP → HoistLift → Dispatched；错误抬升反馈不进入 FromDesEp
        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-BAD", "Fw.H1-L1-AP", "OK", leg.Id));
        hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Stage.Should().Be(FwHoistStage.HoistLift);

        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-BAD", "Fw.H1-L2-EP", "DEVICE_ERR", leg.Id));
        hoist = await db.FwHoistTasks.SingleAsync();
        hoist.Stage.Should().Be(FwHoistStage.HoistLift);
        hoist.Status.Should().Be(FwHoistTaskStatus.Failed);
        exec = await db.FwHoistExecTasks.SingleAsync();
        exec.Status.Should().Be(FwHoistExecStatus.Failed);
    }

    [Fact]
    public async Task HoistSudrPoint_ShouldNotFalseReject()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        db.FwRequestPoints.Add(new FwRequestPoint
        {
            Code = "Fw.H1-L1-AP",
            PointType = FwRequestPointType.HoistInboundAp,
            IsEnabled = true,
            LayerCode = "Fw.L01",
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (_, port, _, dest) = CreateStack(db);
        dest.Subscribe();

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-SUDR", "Fw.H1-L1-AP", 100, 200, "OK"));
        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-SUDR", "Fw.H1-L1-AP", 100, 200, "NG"));

        port.RejectedDestinations.Should().BeEmpty();
    }

    [Fact]
    public async Task MultiHoist_ShouldPreferAvailableFreePort()
    {
        var db = CreateDb();
        SeedCrossLayer(db);
        await db.SaveChangesAsync();

        db.FwHoistDevices.Add(new FwHoistDevice
        {
            HoistNo = "H2",
            Name = "提升机2",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        });
        var h1 = await db.FwHoistDevices.SingleAsync(x => x.HoistNo == "H1");
        h1.IsAvailable = false;
        db.FwHoistLayerPoints.AddRange(
            new FwHoistLayerPoint
            {
                LayerCode = "Fw.L01",
                HoistNo = "H2",
                InboundEp = "Fw.H2-L1-EP",
                InboundAp = "Fw.H2-L1-AP",
                OutboundEp = "Fw.H2-L1-OEP",
                OutboundAp = "Fw.H2-L1-OAP",
                CreateDate = DateTime.UtcNow
            },
            new FwHoistLayerPoint
            {
                LayerCode = "Fw.L02",
                HoistNo = "H2",
                InboundEp = "Fw.H2-L2-EP",
                InboundAp = "Fw.H2-L2-AP",
                OutboundEp = "Fw.H2-L2-OEP",
                OutboundAp = "Fw.H2-L2-OAP",
                CreateDate = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var (bus, _, _, dest) = CreateStack(db);
        dest.Subscribe();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("Fw.LOC-L1-01", "MAT-01", 10m, "TP-MH"));
        await db.SaveChangesAsync();

        await bus.CreateTransportOrderAsync(new CreateTransportOrderRequest(
            "TP-MH", "Fw.LOC-L1-01", "Fw.LOC-L2-01", RefType: "FourWayTransfer", WcsPri: 1));

        var hoist = await db.FwHoistTasks.SingleAsync();
        hoist.HoistNo.Should().Be("H2");
        hoist.SrcAddress.Should().Be("Fw.H2-L1-AP");
        hoist.DesAddress.Should().Be("Fw.H2-L2-EP");
    }

    private static (OrchestrationBus Bus, InMemoryEquipmentTriggerPort Port, FourWayWcsPack Pack, FourWayDestinationService Dest)
        CreateStack(SevenDbContext db)
    {
        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var path = new FourWayPathDispatcher(db, port, new FourWayTrafficGuard());
        var hoist = new FourWayHoistOrchestrator(db, port, path);
        var pack = new FourWayWcsPack(db, control, port, path, hoist);
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        var dest = new FourWayDestinationService(
            db, port, new FourWayInboundAllocator(db), path, bus, pack, hoist);
        return (bus, port, pack, dest);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"FwHoistE2E_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void SeedCrossLayer(SevenDbContext db)
    {
        var warehouse = new WmsWarehouse
        {
            Code = "WH-FW-H",
            Name = "四向跨层仓",
            EnabledPackIds = WcsPackIds.FourWay,
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(warehouse);
        db.SaveChanges();

        var l1 = new WmsLayer
        {
            WarehouseId = warehouse.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.L01",
            Name = "L01",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        var l2 = new WmsLayer
        {
            WarehouseId = warehouse.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.L02",
            Name = "L02",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        db.WmsLayers.AddRange(l1, l2);
        db.SaveChanges();

        db.WmsLocations.AddRange(
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                LayerId = l1.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.LOC-L1-01",
                CreateDate = DateTime.UtcNow
            },
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                LayerId = l2.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.LOC-L2-01",
                CreateDate = DateTime.UtcNow
            },
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                LayerId = l1.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.H1-L1-AP",
                CreateDate = DateTime.UtcNow
            },
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                LayerId = l2.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.H1-L2-EP",
                CreateDate = DateTime.UtcNow
            });

        db.FwHoistDevices.Add(new FwHoistDevice
        {
            HoistNo = "H1",
            Name = "提升机1",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        });
        db.FwHoistLayerPoints.AddRange(
            new FwHoistLayerPoint
            {
                LayerCode = "Fw.L01",
                HoistNo = "H1",
                InboundEp = "Fw.H1-L1-EP",
                InboundAp = "Fw.H1-L1-AP",
                OutboundEp = "Fw.H1-L1-OEP",
                OutboundAp = "Fw.H1-L1-OAP",
                CreateDate = DateTime.UtcNow
            },
            new FwHoistLayerPoint
            {
                LayerCode = "Fw.L02",
                HoistNo = "H1",
                InboundEp = "Fw.H1-L2-EP",
                InboundAp = "Fw.H1-L2-AP",
                OutboundEp = "Fw.H1-L2-OEP",
                OutboundAp = "Fw.H1-L2-OAP",
                CreateDate = DateTime.UtcNow
            });
    }
}
