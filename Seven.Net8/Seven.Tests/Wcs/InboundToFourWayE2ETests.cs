using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wcs;

public class InboundToFourWayE2ETests
{
    [Fact]
    public async Task BuildPallet_Allocate_ThenFourWaySimulate_ShouldPutStockAtFwLocation()
    {
        var db = CreateDb();
        SeedWarehouseAndLocations(db);
        SeedFourWayMaster(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var fwPack = new FourWayWcsPack(db, control);
        var stkPack = new StackerWcsPack(db, control);
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([fwPack]), completion);
        var allocator = new FourWayInboundAllocator(db);
        var fwPath = new FourWayPathDispatcher(db, port, new FourWayTrafficGuard());
        var fwDest = new FourWayDestinationService(db, port, allocator, fwPath, bus);
        // 双包共享 TriggerPort：Stacker 不得因 Fw 申请点 Reject
        var stkDest = new StackerDestinationService(
            db,
            port,
            new StackerAisleAllocator(db),
            new StackerLocationAllocator(db),
            new StackerPathDispatcher(db, port),
            bus);
        fwDest.Subscribe();
        stkDest.Subscribe();

        var resolver = new WcsLocationAllocatorResolver(
            [new FourWayLocationSchema()],
            [allocator]);
        var inbound = new InboundOrderService(db, stock, new BusTransportOrderRequest(bus), resolver);

        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-FW-E2E-001",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-FW-01", 10m, FromLocation: "Fw.RECV-01")]));

        await inbound.ApproveAsync(order.Id);
        var detail = await inbound.BuildPalletAsync(order.Id, new BuildPalletRequest(
            LineNo: 1,
            Qty: 10m,
            ContainerCode: "TP-FW-E2E",
            ReceiveLocationCode: "Fw.RECV-01",
            Height: 1,
            Weight: 10));

        detail.TargetLocationCode.Should().Be("Fw.LOC-A1-01");
        detail.AssignedAisle.Should().Be("Fw.A1");
        detail.AssignedLayer.Should().Be("Fw.L01");
        detail.Status.Should().Be(WmsInboundDetailStatus.Transporting);
        detail.PackId.Should().Be(WcsPackIds.FourWay);

        var booked = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-A1-01");
        booked.IsBooked.Should().BeTrue();

        var afterReceive = await db.WmsInboundOrders.Include(x => x.Lines).SingleAsync();
        afterReceive.Status.Should().Be(WmsOrderStatus.Executing);
        var recvStock = await db.WmsStocks.SingleAsync(x => x.Qty > 0);
        recvStock.LocationCode.Should().Be("Fw.RECV-01");
        recvStock.Qty.Should().Be(10m);

        var transport = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync();
        transport.FromLocationCode.Should().Be("Fw.RECV-01");
        transport.ToLocationCode.Should().Be("Fw.LOC-A1-01");
        transport.RefType.Should().Be("InboundDetail");
        transport.RefId.Should().Be(detail.Id.ToString());
        var leg = transport.Legs.Single();
        leg.PackId.Should().Be(WcsPackIds.FourWay);

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-FW-E2E", "RP_FW_AISLE", 1, 10, "OK"));
        port.RejectedDestinations.Should().BeEmpty();
        port.DispatchedDestinations.Should().ContainSingle(x =>
            x.ContainerCode == "TP-FW-E2E" && x.DestinationPointCode == "EP-Fw.A1");

        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-FW-E2E", "EP-Fw.A1", "OK", leg.Id));

        var afterDone = await db.WmsInboundOrders.AsNoTracking().SingleAsync();
        afterDone.Status.Should().Be(WmsOrderStatus.Completed);

        var target = await db.WmsStocks.SingleAsync(x => x.Qty > 0);
        target.LocationCode.Should().Be("Fw.LOC-A1-01");
        target.Qty.Should().Be(10m);
        target.ContainerCode.Should().Be("TP-FW-E2E");

        var putaway = await db.FwPutAwayTasks.SingleAsync();
        putaway.Status.Should().Be(FwPutAwayStatus.Completed);
        putaway.AssignedLayer.Should().Be("Fw.L01");
        putaway.AssignedAisle.Should().Be("Fw.A1");

        var finishedDetail = await db.WmsInboundDetails.SingleAsync();
        finishedDetail.Status.Should().Be(WmsInboundDetailStatus.Completed);

        var loc = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-A1-01");
        loc.IsBooked.Should().BeFalse();
        loc.IsOccupied.Should().BeTrue();
        loc.CurrentContainerCode.Should().Be("TP-FW-E2E");

        // 双包已注册：纯堆垛腿不被 FourWay 接单
        (await fwPack.CanHandleAsync("Stk.RECV-01", "Stk.LOC-A1-01")).Should().BeFalse();
        (await stkPack.CanHandleAsync("Stk.RECV-01", "Stk.LOC-A1-01")).Should().BeTrue();
    }

    [Fact]
    public async Task DualPack_FourWay_ShouldNotAccept_PureStackerLeg()
    {
        var db = CreateDb();
        var control = new ControlModeService(db);
        var fw = new FourWayWcsPack(db, control);
        var stk = new StackerWcsPack(db, control);

        (await fw.CanHandleAsync("Stk.A", "Stk.B")).Should().BeFalse();
        (await fw.CanHandleAsync("Fw.A", "Fw.B")).Should().BeTrue();
        (await stk.CanHandleAsync("Stk.A", "Stk.B")).Should().BeTrue();
    }

    [Fact]
    public async Task DualPack_FwRequestPoint_CheckNg_ShouldOnlyFourWayReject()
    {
        var db = CreateDb();
        SeedFourWayMaster(db);
        SeedStackerRequestPoint(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var control = new ControlModeService(db);
        var fwPack = new FourWayWcsPack(db, control);
        var (fwDest, stkDest) = CreateDualDest(db, port);
        fwDest.Subscribe();
        stkDest.Subscribe();

        await fwPack.AcceptLegAsync(NewFwLeg("TP-FW-NG", "Fw.RECV", "Fw.LOC-A1-01"));
        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-FW-NG", "RP_FW_AISLE", 1, 10, "NG"));

        port.DispatchedDestinations.Should().BeEmpty();
        port.RejectedDestinations.Should().ContainSingle(x => x.Reason.Contains("校验未通过"));
        (await db.FwPutAwayTasks.SingleAsync(x => x.ContainerCode == "TP-FW-NG"))
            .Status.Should().Be(FwPutAwayStatus.Failed);
    }

    [Fact]
    public async Task DualPack_StkRequestPoint_CheckNg_ShouldOnlyStackerReject()
    {
        var db = CreateDb();
        SeedFourWayMaster(db);
        SeedStackerMasterForNg(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var control = new ControlModeService(db);
        var stkPack = new StackerWcsPack(db, control);
        var (fwDest, stkDest) = CreateDualDest(db, port);
        fwDest.Subscribe();
        stkDest.Subscribe();

        await stkPack.AcceptLegAsync(NewStkLeg("TP-STK-NG", "RECV-01", "Stk.LOC-A1-01"));
        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-STK-NG", "RP_IN_01", 1, 10, "NG"));

        port.DispatchedDestinations.Should().BeEmpty();
        port.RejectedDestinations.Should().ContainSingle(x => x.Reason.Contains("校验未通过"));
        (await db.StkPutAwayTasks.SingleAsync(x => x.ContainerCode == "TP-STK-NG"))
            .Status.Should().Be(StkPutAwayStatus.Failed);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"InboundFwE2E_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void SeedWarehouseAndLocations(SevenDbContext db)
    {
        var warehouse = new WmsWarehouse
        {
            Code = "WH-FW",
            Name = "四向仓",
            EnabledPackIds = WcsPackIds.FourWay,
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(warehouse);
        db.SaveChanges();

        var layer = new WmsLayer
        {
            WarehouseId = warehouse.Id,
            ZoneId = 0,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.L01",
            Name = "L01",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        db.WmsLayers.Add(layer);
        db.SaveChanges();

        var aisle = new WmsAisle
        {
            WarehouseId = warehouse.Id,
            ZoneId = 0,
            LayerId = layer.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.A1",
            Name = "A1",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        db.WmsAisles.Add(aisle);
        db.SaveChanges();

        db.WmsLocations.AddRange(
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.RECV-01",
                CreateDate = DateTime.UtcNow
            },
            new WmsLocation
            {
                WarehouseId = warehouse.Id,
                LayerId = layer.Id,
                AisleId = aisle.Id,
                PackId = WcsPackIds.FourWay,
                Code = "Fw.LOC-A1-01",
                Aisle = "Fw.A1",
                CreateDate = DateTime.UtcNow
            });
    }

    private static void SeedFourWayMaster(SevenDbContext db)
    {
        db.FwLayerPolicies.Add(new FwLayerPolicy
        {
            WarehouseCode = "WH-FW",
            ZoneCode = "Fw.Z",
            LayerCode = "Fw.L01",
            MaxHeight = 9999,
            MaxWeight = 99999,
            IsAvailable = true,
            AllocationWeight = 10,
            CreateDate = DateTime.UtcNow
        });

        db.FwAislePolicies.Add(new FwAislePolicy
        {
            LayerCode = "Fw.L01",
            AisleCode = "Fw.A1",
            MinEmptySlots = 0,
            MaxShuttleCount = 0,
            DestinationPointCode = "EP-Fw.A1",
            AllocationWeight = 1,
            IsAvailable = true,
            MaxHeight = 9999,
            MaxWeight = 99999,
            CreateDate = DateTime.UtcNow
        });

        db.FwRequestPoints.Add(new FwRequestPoint
        {
            Code = "RP_FW_AISLE",
            PointType = FwRequestPointType.AisleRequest,
            IsEnabled = true,
            CreateDate = DateTime.UtcNow
        });
    }

    private static void SeedStackerRequestPoint(SevenDbContext db)
    {
        db.StkRequestPoints.Add(new StkRequestPoint
        {
            Code = "RP_IN_01",
            PointType = StkRequestPointType.AisleRequest,
            IsEnabled = true,
            CreateDate = DateTime.UtcNow
        });
    }

    private static void SeedStackerMasterForNg(SevenDbContext db)
    {
        SeedStackerRequestPoint(db);
        db.StkAssignmentPolicies.Add(new StkAssignmentPolicy
        {
            AisleCode = "A1",
            IsAvailable = true,
            MaxHeight = 3,
            MaxWeight = 100,
            DestinationPointCode = "EP-A1",
            CreateDate = DateTime.UtcNow
        });
        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = 1,
            PackId = WcsPackIds.Stacker,
            Code = "Stk.LOC-A1-01",
            Aisle = "A1",
            CreateDate = DateTime.UtcNow
        });
    }

    private static (FourWayDestinationService Fw, StackerDestinationService Stk) CreateDualDest(
        SevenDbContext db,
        IEquipmentTriggerPort port)
    {
        var fwDest = new FourWayDestinationService(
            db,
            port,
            new FourWayInboundAllocator(db),
            new FourWayPathDispatcher(db, port, new FourWayTrafficGuard()));
        var stkDest = new StackerDestinationService(
            db,
            port,
            new StackerAisleAllocator(db),
            new StackerLocationAllocator(db),
            new StackerPathDispatcher(db, port));
        return (fwDest, stkDest);
    }

    private static TransportLegDto NewFwLeg(string container, string from, string to)
        => new(Guid.NewGuid(), Guid.NewGuid(), WcsPackIds.FourWay, 1, from, to, container, null, null);

    private static TransportLegDto NewStkLeg(string container, string from, string to)
        => new(Guid.NewGuid(), Guid.NewGuid(), WcsPackIds.Stacker, 1, from, to, container, null, null);
}
