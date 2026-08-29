using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

public class FourWayParkingTests
{
    [Fact]
    public async Task ConcurrentReserve_ShouldNotDoubleOccupySameSpot()
    {
        var dbName = $"FwParkConc_{Guid.NewGuid():N}";
        await using var seed = CreateDb(dbName);
        seed.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-ONLY",
            LocationCode = "Fw.PARK-01",
            Status = FwParkingStatus.Free,
            CreateDate = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        await using var db1 = CreateDb(dbName);
        await using var db2 = CreateDb(dbName);
        var port1 = new InMemoryEquipmentTriggerPort();
        var port2 = new InMemoryEquipmentTriggerPort();
        var pack1 = new FourWayWcsPack(db1, new AlwaysAllowControlMode(), port1, new FourWayPathDispatcher(db1, port1, new FourWayTrafficGuard()));
        var pack2 = new FourWayWcsPack(db2, new AlwaysAllowControlMode(), port2, new FourWayPathDispatcher(db2, port2, new FourWayTrafficGuard()));

        var leg1 = Guid.NewGuid();
        var leg2 = Guid.NewGuid();

        await Task.WhenAll(
            pack1.AcceptLegAsync(OutboundLeg(leg1, "TP-A", "Fw.LOC-A", "Fw.DOCK")),
            pack2.AcceptLegAsync(OutboundLeg(leg2, "TP-B", "Fw.LOC-B", "Fw.DOCK")));

        await using var check = CreateDb(dbName);
        var ledgers = await check.FwParkingLedgers.ToListAsync();
        ledgers.Should().ContainSingle();
        ledgers[0].Status.Should().BeOneOf(FwParkingStatus.Reserved, FwParkingStatus.Occupied);
        ledgers[0].OwnerId.Should().NotBeNull();

        var tasks = await check.FwRetrievalTasks.ToListAsync();
        tasks.Should().HaveCount(2);
        tasks.Count(x => x.Status == FwRetrievalStatus.Dispatched).Should().Be(1);
        tasks.Count(x => x.Status == FwRetrievalStatus.Suspended).Should().Be(1);
        tasks.Single(x => x.Status == FwRetrievalStatus.Dispatched).Id.Should().Be(ledgers[0].OwnerId!.Value);
    }

    [Fact]
    public async Task WakeSuspended_AfterFreeParking_ShouldDispatch()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var path = new FourWayPathDispatcher(db, port, new FourWayTrafficGuard());
        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, path);

        var leg = Guid.NewGuid();
        await pack.AcceptLegAsync(OutboundLeg(leg, "TP-WAKE", "Fw.LOC-A", "Fw.DOCK"));
        var retrieval = await db.FwRetrievalTasks.SingleAsync();
        await pack.TryDispatchRetrievalAsync(retrieval);
        retrieval.Status.Should().Be(FwRetrievalStatus.Suspended);

        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-WAKE",
            LocationCode = "Fw.PARK-01",
            Status = FwParkingStatus.Free,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var woken = await pack.WakeSuspendedRetrievalsAsync();
        woken.Should().Be(1);

        retrieval = await db.FwRetrievalTasks.SingleAsync();
        retrieval.Status.Should().Be(FwRetrievalStatus.Dispatched);
        (await db.FwParkingLedgers.SingleAsync()).Status.Should().Be(FwParkingStatus.Occupied);
        port.DispatchedDestinations.Should().ContainSingle(x => x.ContainerCode == "TP-WAKE");
    }

    [Fact]
    public async Task SelectAisle_ShouldSkip_WhenMaxShuttleReached()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-FULL", minEmpty: 0, maxShuttle: 1, weight: 100);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-OPEN", minEmpty: 0, maxShuttle: 1, weight: 1);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-FULL", "Fw.N-FULL-01");
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-OPEN", "Fw.N-OPEN-01");
        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-FULL",
            LocationCode = "Fw.PARK-FULL",
            LayerCode = "Fw.L01",
            AisleCode = "Fw.A-FULL",
            Status = FwParkingStatus.Occupied,
            OwnerId = Guid.NewGuid(),
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeTrue();
        result.AisleCode.Should().Be("Fw.A-OPEN");
        result.LocationCode.Should().Be("Fw.N-OPEN-01");
    }

    [Fact]
    public async Task Dispatch_ShouldMarkParkingOccupied_ReleaseOnComplete()
    {
        var db = CreateDb();
        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-OCC",
            Status = FwParkingStatus.Free,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, new FourWayPathDispatcher(db, port, new FourWayTrafficGuard()));
        var leg = Guid.NewGuid();
        await pack.AcceptLegAsync(OutboundLeg(leg, "TP-OCC", "Fw.LOC-A", "Fw.DOCK"));
        var retrieval = await db.FwRetrievalTasks.SingleAsync();
        await pack.TryDispatchRetrievalAsync(retrieval);

        (await db.FwParkingLedgers.SingleAsync()).Status.Should().Be(FwParkingStatus.Occupied);

        await pack.ReleaseParkingAsync(retrieval.Id);
        var free = await db.FwParkingLedgers.SingleAsync();
        free.Status.Should().Be(FwParkingStatus.Free);
        free.OwnerId.Should().BeNull();
    }

    [Fact]
    public async Task Dispatch_WhenPortThrows_ShouldReleaseParkingAndSuspend()
    {
        var db = CreateDb();
        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-THROW",
            LocationCode = "Fw.PARK-01",
            Status = FwParkingStatus.Free,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var port = new ThrowingDispatchPort();
        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, new FourWayPathDispatcher(db, port, new FourWayTrafficGuard()));
        var leg = Guid.NewGuid();
        await pack.AcceptLegAsync(OutboundLeg(leg, "TP-THROW", "Fw.LOC-A", "Fw.DOCK"));

        var retrieval = await db.FwRetrievalTasks.SingleAsync();
        retrieval.Status.Should().Be(FwRetrievalStatus.Suspended);

        var parking = await db.FwParkingLedgers.SingleAsync();
        parking.Status.Should().Be(FwParkingStatus.Free);
        parking.OwnerId.Should().BeNull();
    }

    [Fact]
    public async Task Dispatch_WhenGrantBlocked_ShouldKeepReservedNotOccupiedOrDispatched()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (_, routeAc, _) = await SeedLinearMapAsync(db);

        (await guard.TryGrantAsync(routeAc.Id.ToString(), "A", "C", "blocker")).Should().BeTrue();

        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-ROUTE",
            LocationCode = "Fw.PARK-01",
            Status = FwParkingStatus.Free,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, new FourWayPathDispatcher(db, port, guard));
        var leg = Guid.NewGuid();
        await pack.AcceptLegAsync(OutboundLeg(leg, "TP-ROUTE", "A", "B"));

        var retrieval = await db.FwRetrievalTasks.SingleAsync();
        retrieval.Status.Should().NotBe(FwRetrievalStatus.Dispatched);
        retrieval.Status.Should().BeOneOf(FwRetrievalStatus.Accepted, FwRetrievalStatus.Suspended);

        var parking = await db.FwParkingLedgers.SingleAsync();
        parking.Status.Should().Be(FwParkingStatus.Reserved);
        parking.OwnerId.Should().Be(retrieval.Id);

        (await db.FwShuttleTasks.SingleAsync()).Status.Should().Be(FwShuttleTaskStatus.Routing);
        port.DispatchedDestinations.Should().BeEmpty();

        await guard.ReleaseAsync(routeAc.Id.ToString(), "blocker");
        var path = new FourWayPathDispatcher(db, port, guard);
        (await path.RetryStuckRoutingAsync()).Should().Be(1);
        (await pack.PromoteRetrievalsAfterPathGrantAsync()).Should().Be(1);

        retrieval = await db.FwRetrievalTasks.SingleAsync();
        retrieval.Status.Should().Be(FwRetrievalStatus.Dispatched);
        (await db.FwParkingLedgers.SingleAsync()).Status.Should().Be(FwParkingStatus.Occupied);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "C");
    }

    [Fact]
    public async Task Reserve_ShouldStampLayerAisle_FromRetrievalFromLocation()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-01", "Fw.LOC-STAMP");
        db.FwParkingLedgers.Add(new FwParkingLedger
        {
            Code = "PK-STAMP",
            LocationCode = "Fw.PARK-NONE",
            Status = FwParkingStatus.Free,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, new FourWayPathDispatcher(db, port, new FourWayTrafficGuard()));
        await pack.AcceptLegAsync(OutboundLeg(Guid.NewGuid(), "TP-STAMP", "Fw.LOC-STAMP", "Fw.DOCK"));

        var parking = await db.FwParkingLedgers.SingleAsync();
        parking.LayerCode.Should().Be("Fw.L01");
        parking.AisleCode.Should().Be("Fw.A-01");
        parking.Status.Should().Be(FwParkingStatus.Occupied);
    }

    private static TransportLegDto OutboundLeg(Guid legId, string container, string from, string to)
        => new(legId, Guid.NewGuid(), WcsPackIds.FourWay, 1, from, to, container,
            null, null, RefType: "OutboundOrder", WcsGroupNo: $"G-{container}", WcsPri: 1);

    private static async Task<(FwMapVersion Map, FwRoute RouteAc, FwRoute RouteCb)> SeedLinearMapAsync(SevenDbContext db)
    {
        var map = new FwMapVersion
        {
            Code = "MAP-PARK",
            Name = "MAP-PARK",
            IsActive = true,
            LayerCode = "Fw.L01",
            CreateDate = DateTime.UtcNow
        };
        db.FwMapVersions.Add(map);
        await db.SaveChangesAsync();

        db.FwNodes.AddRange(
            new FwNode { MapVersionId = map.Id, Code = "A", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = map.Id, Code = "C", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = map.Id, Code = "B", CreateDate = DateTime.UtcNow });
        db.FwRoutes.AddRange(
            new FwRoute
            {
                MapVersionId = map.Id, FromCode = "A", ToCode = "C", Weight = 1, Capacity = 1,
                CreateDate = DateTime.UtcNow
            },
            new FwRoute
            {
                MapVersionId = map.Id, FromCode = "C", ToCode = "B", Weight = 1, Capacity = 1,
                CreateDate = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var routeAc = await db.FwRoutes.SingleAsync(x => x.MapVersionId == map.Id && x.FromCode == "A");
        var routeCb = await db.FwRoutes.SingleAsync(x => x.MapVersionId == map.Id && x.FromCode == "C");
        return (map, routeAc, routeCb);
    }

    private static SevenDbContext CreateDb(string? name = null)
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase(name ?? $"FwPark_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class ThrowingDispatchPort : IEquipmentTriggerPort
    {
#pragma warning disable CS0067
        public event Func<DestinationRequestTrigger, Task>? DestinationRequested;
        public event Func<DeviceSegmentFeedback, Task>? SegmentFeedback;
#pragma warning restore CS0067

        public Task DispatchDestinationAsync(DispatchDestinationCommand cmd, CancellationToken ct = default)
            => throw new InvalidOperationException("dispatch failed");

        public Task RejectDestinationAsync(RejectDestinationCommand cmd, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DispatchMoveAsync(DispatchMoveCommand cmd, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SimulateDestinationRequestAsync(DestinationRequestTrigger trigger, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SimulateSegmentFeedbackAsync(DeviceSegmentFeedback feedback, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class AlwaysAllowControlMode : Seven.Application.Platform.IControlModeService
    {
        public Task<Seven.Application.Platform.ControlModeState> GetAsync(string scope, CancellationToken ct = default)
            => Task.FromResult(new Seven.Application.Platform.ControlModeState(
                scope, WcsControlMode.Auto, false, DateTime.UtcNow));

        public Task SetModeAsync(string scope, WcsControlMode mode, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SetEStopAsync(string scope, bool eStop, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> CanAcceptLegsAsync(string packId, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private static WmsWarehouse SeedWarehouse(SevenDbContext db)
    {
        var wh = new WmsWarehouse
        {
            Code = "WH-FW-PARK",
            Name = "parking",
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
        int minEmpty,
        int maxShuttle,
        int weight)
    {
        db.FwAislePolicies.Add(new FwAislePolicy
        {
            LayerCode = layerCode,
            AisleCode = aisleCode,
            MinEmptySlots = minEmpty,
            MaxShuttleCount = maxShuttle,
            DestinationPointCode = $"EP-{aisleCode}",
            AllocationWeight = weight,
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
