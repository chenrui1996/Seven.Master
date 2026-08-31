using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

public class FourWayPathDispatcherTests
{
    [Fact]
    public async Task Dispatch_WithMap_ShouldGrantFirstEdge_AndAdvanceReleasesThenGrantsNext()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();

        var map = new FwMapVersion
        {
            Code = "MAP-L01",
            Name = "L01",
            IsActive = true,
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
                MapVersionId = map.Id,
                FromCode = "A",
                ToCode = "C",
                Weight = 1,
                Capacity = 1,
                CreateDate = DateTime.UtcNow
            },
            new FwRoute
            {
                MapVersionId = map.Id,
                FromCode = "C",
                ToCode = "B",
                Weight = 1,
                Capacity = 1,
                CreateDate = DateTime.UtcNow
            });

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-PATH",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var routeAc = await db.FwRoutes.SingleAsync(x => x.FromCode == "A" && x.ToCode == "C");
        var routeCb = await db.FwRoutes.SingleAsync(x => x.FromCode == "C" && x.ToCode == "B");

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        var paths = await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-PATH", legId, shuttleId, map.Id));

        paths.Should().HaveCount(3);
        paths.Select(x => x.NodeCode).Should().Equal("A", "C", "B");
        paths[1].EdgeId.Should().Be(routeAc.Id.ToString());
        paths[2].EdgeId.Should().Be(routeCb.Id.ToString());

        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "C");
        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Running);

        // 首段边已占用：对向拒收
        (await guard.TryGrantAsync(routeAc.Id.ToString(), "C", "A", "other")).Should().BeFalse();

        var shuttle = await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId);
        var advanced = await dispatcher.AdvanceAfterSegmentAsync(shuttle, "C");
        advanced.Should().BeTrue();
        port.DispatchedDestinations.Should().Contain(x => x.DestinationPointCode == "B");

        // 首段已释放，可再占；第二段被本任务占用
        (await guard.TryGrantAsync(routeAc.Id.ToString(), "A", "C", "other2")).Should().BeTrue();
        (await guard.TryGrantAsync(routeCb.Id.ToString(), "B", "C", "other3")).Should().BeFalse();

        advanced = await dispatcher.AdvanceAfterSegmentAsync(shuttle, "B");
        advanced.Should().BeFalse();
        (await guard.TryGrantAsync(routeCb.Id.ToString(), "C", "B", "other4")).Should().BeTrue();
    }

    [Fact]
    public async Task Dispatch_WithoutMap_ShouldFallbackSingleSegment()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-FB",
            FromCode = "RP",
            ToCode = "EP",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        var paths = await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "RP", "EP", "TP-FB", legId, shuttleId));

        paths.Should().HaveCount(2);
        paths.Last().NodeCode.Should().Be("EP");
        paths.Should().OnlyContain(x => x.EdgeId == null);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "EP");

        var shuttle = await db.FwShuttleTasks.SingleAsync();
        shuttle.Status.Should().Be(FwShuttleTaskStatus.Running);
        (await dispatcher.AdvanceAfterSegmentAsync(shuttle, "EP")).Should().BeFalse();
    }

    [Fact]
    public async Task RetryStuckRouting_AfterGrantFreed_ShouldDispatch()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (map, routeAc, _) = await SeedLinearMapAsync(db, "MAP-RETRY", layerCode: "Fw.L01");

        // 先被其他车占用首边
        (await guard.TryGrantAsync(routeAc.Id.ToString(), "A", "C", "blocker")).Should().BeTrue();

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-RETRY",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-RETRY", legId, shuttleId, map.Id, "Fw.L01"));

        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Routing);
        port.DispatchedDestinations.Should().BeEmpty();

        await guard.ReleaseAsync(routeAc.Id.ToString(), "blocker");
        var resumed = await dispatcher.RetryStuckRoutingAsync();
        resumed.Should().Be(1);

        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Running);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "C");
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), shuttleId.ToString("N"))).Should().BeTrue();
    }

    [Fact]
    public async Task CancelLeg_ShouldReleaseAllFlowEdges()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (map, routeAc, _) = await SeedLinearMapAsync(db, "MAP-CANCEL", layerCode: "Fw.L01");

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwPutAwayTasks.Add(new FwPutAwayTask
        {
            Id = Guid.NewGuid(),
            LegId = legId,
            ContainerCode = "TP-CANCEL",
            FromCode = "A",
            ToCode = "B",
            Status = FwPutAwayStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-CANCEL",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-CANCEL", legId, shuttleId, map.Id));

        var ownerId = shuttleId.ToString("N");
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), ownerId)).Should().BeTrue();
        guard.HasAnyFallbackHold(ownerId).Should().BeTrue();

        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, dispatcher);
        await pack.CancelLegAsync(legId);

        guard.HasAnyFallbackHold(ownerId).Should().BeFalse();
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), ownerId)).Should().BeFalse();
        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Cancelled);
    }

    [Fact]
    public async Task RejectMarkFailed_ShouldReleaseAllFlowEdges()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (map, routeAc, _) = await SeedLinearMapAsync(db, "MAP-FAIL", layerCode: "Fw.L01");

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwPutAwayTasks.Add(new FwPutAwayTask
        {
            Id = Guid.NewGuid(),
            LegId = legId,
            ContainerCode = "TP-FAIL",
            FromCode = "A",
            ToCode = "B",
            Status = FwPutAwayStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-FAIL",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-FAIL", legId, shuttleId, map.Id));

        var ownerId = shuttleId.ToString("N");
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), ownerId)).Should().BeTrue();

        var dest = new FourWayDestinationService(db, port, new FourWayInboundAllocator(db), dispatcher);
        await dest.HandleDestinationRequestedAsync(new DestinationRequestTrigger(
            "TP-FAIL", "RP_FW_FAIL", 1, 10, "NG"));

        guard.HasAnyFallbackHold(ownerId).Should().BeFalse();
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), ownerId)).Should().BeFalse();
        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Failed);
        (await db.FwPutAwayTasks.SingleAsync(x => x.LegId == legId))
            .Status.Should().Be(FwPutAwayStatus.Failed);
        port.RejectedDestinations.Should().ContainSingle(x => x.Reason.Contains("校验未通过"));
    }

    [Fact]
    public async Task FailLeg_Retrieval_ShouldReleaseAllFlowEdges()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (map, routeAc, _) = await SeedLinearMapAsync(db, "MAP-FAIL-RET", layerCode: "Fw.L01");

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwRetrievalTasks.Add(new FwRetrievalTask
        {
            Id = Guid.NewGuid(),
            LegId = legId,
            ContainerCode = "TP-FAIL-R",
            FromCode = "A",
            ToCode = "B",
            WcsGroupNo = "G1",
            WcsPri = 1,
            Status = FwRetrievalStatus.Dispatched,
            CreateDate = DateTime.UtcNow
        });
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-FAIL-R",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-FAIL-R", legId, shuttleId, map.Id));

        var ownerId = shuttleId.ToString("N");
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), ownerId)).Should().BeTrue();

        var pack = new FourWayWcsPack(db, new AlwaysAllowControlMode(), port, dispatcher);
        await pack.FailLegAsync(legId);

        guard.HasAnyFallbackHold(ownerId).Should().BeFalse();
        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), ownerId)).Should().BeFalse();
        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Failed);
        (await db.FwRetrievalTasks.SingleAsync(x => x.LegId == legId))
            .Status.Should().Be(FwRetrievalStatus.Failed);
    }

    [Fact]
    public async Task TryResumeGrant_WhenAlreadyRunning_ShouldNotReDispatch()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (map, routeAc, _) = await SeedLinearMapAsync(db, "MAP-IDEM", layerCode: "Fw.L01");

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-IDEM",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-IDEM", legId, shuttleId, map.Id));

        port.DispatchedDestinations.Should().ContainSingle();
        var shuttle = await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId);
        shuttle.Status.Should().Be(FwShuttleTaskStatus.Running);

        (await dispatcher.TryResumeGrantAsync(shuttle)).Should().BeFalse();
        port.DispatchedDestinations.Should().ContainSingle();
    }

    [Fact]
    public async Task TryResumeGrant_WhenAlreadyHeldButRouting_ShouldSkipReDispatch()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var (map, routeAc, _) = await SeedLinearMapAsync(db, "MAP-HELD", layerCode: "Fw.L01");

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-HELD",
            FromCode = "A",
            ToCode = "B",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-HELD", legId, shuttleId, map.Id));

        var shuttle = await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId);
        shuttle.Status = FwShuttleTaskStatus.Routing;
        await db.SaveChangesAsync();
        port.DispatchedDestinations.Clear();

        (await guard.IsHeldByOwnerAsync(routeAc.Id.ToString(), shuttleId.ToString("N"))).Should().BeTrue();
        (await dispatcher.TryResumeGrantAsync(shuttle)).Should().BeTrue();

        (await db.FwShuttleTasks.SingleAsync(x => x.Id == shuttleId))
            .Status.Should().Be(FwShuttleTaskStatus.Running);
        port.DispatchedDestinations.Should().BeEmpty();
    }

    [Fact]
    public async Task TwoLayers_TwoMaps_ShouldIsolateRoutesByLayerCode()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();

        var mapL1 = new FwMapVersion
        {
            Code = "MAP-L1",
            Name = "L1",
            IsActive = true,
            LayerCode = "Fw.L01",
            CreateDate = DateTime.UtcNow
        };
        var mapL2 = new FwMapVersion
        {
            Code = "MAP-L2",
            Name = "L2",
            IsActive = true,
            LayerCode = "Fw.L02",
            CreateDate = DateTime.UtcNow
        };
        db.FwMapVersions.AddRange(mapL1, mapL2);
        await db.SaveChangesAsync();

        // L1: A→C→B；L2: A→X→B（不同中间点，便于断言边归属）
        db.FwNodes.AddRange(
            new FwNode { MapVersionId = mapL1.Id, Code = "A", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = mapL1.Id, Code = "C", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = mapL1.Id, Code = "B", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = mapL2.Id, Code = "A", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = mapL2.Id, Code = "X", CreateDate = DateTime.UtcNow },
            new FwNode { MapVersionId = mapL2.Id, Code = "B", CreateDate = DateTime.UtcNow });
        db.FwRoutes.AddRange(
            new FwRoute
            {
                MapVersionId = mapL1.Id, FromCode = "A", ToCode = "C", Weight = 1, Capacity = 1,
                CreateDate = DateTime.UtcNow
            },
            new FwRoute
            {
                MapVersionId = mapL1.Id, FromCode = "C", ToCode = "B", Weight = 1, Capacity = 1,
                CreateDate = DateTime.UtcNow
            },
            new FwRoute
            {
                MapVersionId = mapL2.Id, FromCode = "A", ToCode = "X", Weight = 1, Capacity = 1,
                CreateDate = DateTime.UtcNow
            },
            new FwRoute
            {
                MapVersionId = mapL2.Id, FromCode = "X", ToCode = "B", Weight = 1, Capacity = 1,
                CreateDate = DateTime.UtcNow
            });

        var shuttle1 = Guid.NewGuid();
        var shuttle2 = Guid.NewGuid();
        var leg1 = Guid.NewGuid();
        var leg2 = Guid.NewGuid();
        db.FwShuttleTasks.AddRange(
            new FwShuttleTask
            {
                Id = shuttle1, LegId = leg1, ContainerCode = "TP-L1", FromCode = "A", ToCode = "B",
                Status = FwShuttleTaskStatus.Accepted, CreateDate = DateTime.UtcNow
            },
            new FwShuttleTask
            {
                Id = shuttle2, LegId = leg2, ContainerCode = "TP-L2", FromCode = "A", ToCode = "B",
                Status = FwShuttleTaskStatus.Accepted, CreateDate = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var routeL1Ac = await db.FwRoutes.SingleAsync(x => x.MapVersionId == mapL1.Id && x.FromCode == "A");
        var routeL2Ax = await db.FwRoutes.SingleAsync(x => x.MapVersionId == mapL2.Id && x.FromCode == "A");

        var dispatcher = new FourWayPathDispatcher(db, port, guard);
        var pathsL1 = await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-L1", leg1, shuttle1, LayerCode: "Fw.L01"));
        var pathsL2 = await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "A", "B", "TP-L2", leg2, shuttle2, LayerCode: "Fw.L02"));

        pathsL1.Select(x => x.NodeCode).Should().Equal("A", "C", "B");
        pathsL2.Select(x => x.NodeCode).Should().Equal("A", "X", "B");
        pathsL1[1].EdgeId.Should().Be(routeL1Ac.Id.ToString());
        pathsL2[1].EdgeId.Should().Be(routeL2Ax.Id.ToString());
        pathsL1[1].EdgeId.Should().NotBe(pathsL2[1].EdgeId);

        (await dispatcher.ResolveMapVersionIdAsync(null, "Fw.L01")).Should().Be(mapL1.Id);
        (await dispatcher.ResolveMapVersionIdAsync(null, "Fw.L02")).Should().Be(mapL2.Id);
    }

    private static async Task<(FwMapVersion Map, FwRoute RouteAc, FwRoute RouteCb)> SeedLinearMapAsync(
        SevenDbContext db,
        string mapCode,
        string? layerCode = null)
    {
        var map = new FwMapVersion
        {
            Code = mapCode,
            Name = mapCode,
            IsActive = true,
            LayerCode = layerCode,
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

    private sealed class AlwaysAllowControlMode : Seven.Application.Platform.IControlModeService
    {
        public Task<Seven.Application.Platform.ControlModeState> GetAsync(string scope, CancellationToken ct = default)
            => Task.FromResult(new Seven.Application.Platform.ControlModeState(
                scope, Seven.Domain.Enums.WcsControlMode.Auto, false, DateTime.UtcNow));

        public Task SetModeAsync(string scope, Seven.Domain.Enums.WcsControlMode mode, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SetEStopAsync(string scope, bool eStop, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> CanAcceptLegsAsync(string packId, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"FwPath_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
