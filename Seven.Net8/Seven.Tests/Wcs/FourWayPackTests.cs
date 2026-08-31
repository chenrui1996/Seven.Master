using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

public class FourWayPackTests
{
    [Fact]
    public void Router_ShouldFindPath_OnThreeNodeGraph()
    {
        var edges = new[]
        {
            new FourWayEdge("A", "C", Weight: 1),
            new FourWayEdge("C", "B", Weight: 1)
        };

        var path = FourWayRouter.FindPath("A", "B", edges);

        path.Should().Equal("A", "C", "B");
    }

    [Fact]
    public async Task Traffic_ShouldReject_HeadOnOppositeDirection()
    {
        var guard = new FourWayTrafficGuard();

        var first = await guard.TryGrantAsync("e-ab", fromNode: "A", toNode: "B", ownerId: "v1");
        var opposite = await guard.TryGrantAsync("e-ab", fromNode: "B", toNode: "A", ownerId: "v2");

        first.Should().BeTrue();
        opposite.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptLeg_ShouldCreatePutAwayAndShuttle_WhenCanHandle()
    {
        var db = CreateDb();
        var pack = new FourWayWcsPack(db, new ControlModeService(db));

        pack.PackId.Should().Be(WcsPackIds.FourWay);
        (await pack.CanHandleAsync("Fw.A", "Fw.B")).Should().BeTrue();
        (await pack.CanHandleAsync("Stk.A", "Stk.B")).Should().BeFalse();

        var leg = new TransportLegDto(
            Guid.NewGuid(), Guid.NewGuid(), WcsPackIds.FourWay, 1, "Fw.A", "Fw.B", "TP-FW-1", null, null);

        var result = await pack.AcceptLegAsync(leg);

        result.Accepted.Should().BeTrue();
        var putaway = await db.FwPutAwayTasks.SingleAsync(x => x.LegId == leg.LegId);
        putaway.ContainerCode.Should().Be("TP-FW-1");
        putaway.Status.Should().Be(FwPutAwayStatus.Accepted);
        var shuttle = await db.FwShuttleTasks.SingleAsync(x => x.LegId == leg.LegId);
        shuttle.ContainerCode.Should().Be("TP-FW-1");
        shuttle.Status.Should().Be(FwShuttleTaskStatus.Accepted);
    }

    [Fact]
    public async Task CanHandle_ShouldAllow_HandoverEnd()
    {
        var db = CreateDb();
        db.WmsHandoverLinks.Add(new WmsHandoverLink
        {
            FromPackId = WcsPackIds.Stacker,
            ToPackId = WcsPackIds.FourWay,
            LocationCode = "HO-FW-01",
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new ControlModeService(db));
        (await pack.CanHandleAsync("HO-FW-01", "Fw.BIN-01")).Should().BeTrue();
        (await pack.CanHandleAsync("RECV", "Fw.BIN-01")).Should().BeFalse();
    }

    [Fact]
    public async Task SimulateDestinationRequest_CheckNg_ShouldReject()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        SeedFourWayMaster(db);
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new ControlModeService(db));
        var dest = CreateDest(db, port);
        dest.Subscribe();

        await pack.AcceptLegAsync(NewLeg("TP-NG", "Fw.RECV", "Fw.LOC-A1-01"));
        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-NG", "RP_FW_AISLE", 1, 10, "NG"));

        port.DispatchedDestinations.Should().BeEmpty();
        port.RejectedDestinations.Should().ContainSingle(x => x.Reason.Contains("校验未通过"));
        (await db.FwPutAwayTasks.SingleAsync(x => x.ContainerCode == "TP-NG"))
            .Status.Should().Be(FwPutAwayStatus.Failed);
    }

    [Fact]
    public async Task AisleRequest_ShouldDispatchEpPoint()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        SeedFourWayMaster(db);
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new ControlModeService(db));
        var dest = CreateDest(db, port);
        dest.Subscribe();

        var leg = NewLeg("TP-AISLE", "Fw.RECV", "Fw.LOC-A1-01");
        var accepted = await pack.AcceptLegAsync(leg);
        accepted.Accepted.Should().BeTrue();

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-AISLE", "RP_FW_AISLE", 1, 10, "OK"));

        port.DispatchedDestinations.Should().ContainSingle();
        var cmd = port.DispatchedDestinations.Single();
        cmd.ContainerCode.Should().Be("TP-AISLE");
        cmd.DestinationPointCode.Should().Be("EP-Fw.A1");
        cmd.LegId.Should().Be(leg.LegId);

        var putaway = await db.FwPutAwayTasks.SingleAsync(x => x.ContainerCode == "TP-AISLE");
        putaway.Status.Should().Be(FwPutAwayStatus.AisleAssigned);
        putaway.AssignedLayer.Should().Be("Fw.L01");
        putaway.AssignedAisle.Should().Be("Fw.A1");
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Fw_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static FourWayDestinationService CreateDest(SevenDbContext db, IEquipmentTriggerPort port)
    {
        var guard = new FourWayTrafficGuard();
        var path = new FourWayPathDispatcher(db, port, guard);
        return new FourWayDestinationService(db, port, new FourWayInboundAllocator(db), path);
    }

    private static TransportLegDto NewLeg(string container, string from, string to)
        => new(Guid.NewGuid(), Guid.NewGuid(), WcsPackIds.FourWay, 1, from, to, container, null, null);

    private static void SeedFourWayMaster(SevenDbContext db)
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

        var layer = new WmsLayer
        {
            WarehouseId = wh.Id,
            ZoneId = 0,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.L01",
            Name = "L01",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        db.WmsLayers.Add(layer);
        db.SaveChanges();

        db.WmsAisles.Add(new WmsAisle
        {
            WarehouseId = wh.Id,
            ZoneId = 0,
            LayerId = layer.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.A1",
            Name = "A1",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        });

        db.FwLayerPolicies.Add(new FwLayerPolicy
        {
            WarehouseCode = wh.Code,
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

        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = wh.Id,
            LayerId = layer.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.LOC-A1-01",
            Aisle = "Fw.A1",
            CreateDate = DateTime.UtcNow
        });
    }
}
