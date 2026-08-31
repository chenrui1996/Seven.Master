using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Simulator;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Simulator;

namespace Seven.Tests.Simulator;

public class SimulationDeployServiceTests
{
    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"SimDeploy_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static SimProjectDto SampleProject(string name = "Demo") => new(
        1,
        new SimProjectMetaDto(
            name,
            new SimFeaturesDto(true, true, false, false, true, new SimWcsPacksDto(true, false, false)),
            "Simulation"),
        new SimMapDto(
            "stacker",
            [
                new SimMapNodeDto("1", "RECV-01", 0, 0),
                new SimMapNodeDto("2", "BIN-01", 100, 0)
            ],
            []));

    [Fact]
    public void ValidateFeatures_ShouldWarn_WhenWmsOff()
    {
        var svc = new SimulationDeployService(CreateDb());
        var result = svc.ValidateFeatures(new SimFeaturesDto(false, false, false, false, true, new SimWcsPacksDto(true, false, false)));
        result.Ok.Should().BeTrue();
        result.Warnings.Should().Contain(e => e.Contains("Wms"));
    }

    [Fact]
    public async Task Deploy_ShouldCreateWarehouseLocations_AndRequestPoints()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);

        var result = await svc.DeployAsync(SampleProject());

        result.DeploymentId.Should().BeGreaterThan(0);
        result.WarehouseCode.Should().Be("SIM_DEMO");
        result.LocationCount.Should().Be(2);

        (await db.WmsWarehouses.CountAsync()).Should().Be(1);
        var wh = await db.WmsWarehouses.SingleAsync();
        wh.EnabledPackIds.Should().Be("stacker");
        (await db.WmsLocations.CountAsync()).Should().Be(2);
        (await db.WmsLocations.Select(x => x.Code).OrderBy(x => x).ToListAsync())
            .Should().Equal("Stk.BIN-01", "Stk.RECV-01");
        (await db.WmsLocations.AllAsync(x => x.PackId == "stacker")).Should().BeTrue();
        (await db.StkRequestPoints.CountAsync()).Should().Be(2);
        (await db.SimDeployments.CountAsync(x => x.Status == "Deployed")).Should().Be(1);
    }

    [Fact]
    public async Task Undeploy_ShouldMarkStatus()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject("P1"));

        await svc.UndeployAsync("P1");

        (await db.SimDeployments.SingleAsync()).Status.Should().Be("Undeployed");
        (await db.WmsLocations.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Deploy_Stacker_WithEdgeAndRequestPoint_ShouldCreateRouteAndRequestPoint()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        var project = new SimProjectDto(
            1,
            new SimProjectMetaDto(
                "EdgeDemo",
                new SimFeaturesDto(true, true, false, false, true, new SimWcsPacksDto(true, false, false)),
                "Simulation"),
            new SimMapDto(
                "stacker",
                [
                    new SimMapNodeDto("1", "RECV-01", 0, 0),
                    new SimMapNodeDto("2", "BIN-01", 100, 0)
                ],
                [new SimMapEdgeDto("e1", "RECV-01", "BIN-01")],
                null,
                [new SimMapRequestPointDto("RP-IN", "RECV-01")]));

        var result = await svc.DeployAsync(project);

        result.EdgeCount.Should().Be(1);
        (await db.StkRoutes.CountAsync()).Should().Be(1);
        var route = await db.StkRoutes.SingleAsync();
        route.FromCode.Should().Be("Stk.RECV-01");
        route.ToCode.Should().Be("Stk.BIN-01");

        (await db.StkRequestPoints.CountAsync()).Should().Be(1);
        var rp = await db.StkRequestPoints.SingleAsync();
        rp.Code.Should().Be("Stk.RP-IN");
        rp.AisleCode.Should().Be("Stk.RECV-01");
    }

    [Fact]
    public async Task Deploy_ShouldAutoCreateScadaView_WhenViewsEmpty()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        var project = SampleProject("ScadaDemo");

        await svc.DeployAsync(project);

        (await db.ScdViews.CountAsync()).Should().Be(1);
        var view = await db.ScdViews.SingleAsync();
        view.Code.Should().Be("SIM_SCADADEMO");
        view.Name.Should().Be("SIM_SCADADEMO");

        (await db.ScdNodeBinds.CountAsync()).Should().Be(2);
        var binds = await db.ScdNodeBinds.OrderBy(x => x.LocationCode).ToListAsync();
        binds[0].LocationCode.Should().Be("Stk.BIN-01");
        binds[0].X.Should().Be(100);
        binds[1].LocationCode.Should().Be("Stk.RECV-01");
        binds[1].X.Should().Be(0);
    }

    [Fact]
    public async Task Deploy_FourWay_WithEdge_ShouldCreateFwNodeAndRoute()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        var project = new SimProjectDto(
            1,
            new SimProjectMetaDto(
                "FwDemo",
                new SimFeaturesDto(true, true, true, false, true, new SimWcsPacksDto(false, true, false)),
                "Simulation"),
            new SimMapDto(
                "fourway",
                [
                    new SimMapNodeDto("1", "N1", 0, 0),
                    new SimMapNodeDto("2", "N2", 50, 0)
                ],
                [new SimMapEdgeDto("e1", "N1", "N2")]));

        var result = await svc.DeployAsync(project);

        result.EdgeCount.Should().Be(1);
        (await db.FwNodes.CountAsync()).Should().Be(2);
        (await db.FwRoutes.CountAsync()).Should().Be(1);
        var route = await db.FwRoutes.SingleAsync();
        route.FromCode.Should().Be("Fw.N1");
        route.ToCode.Should().Be("Fw.N2");
    }

    [Fact]
    public async Task Reset_AfterDeploy_ShouldKeepDeploymentAndLocations()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject("KeepDemo"));

        await svc.ResetAsync("KeepDemo");

        (await db.SimDeployments.SingleAsync()).Status.Should().Be("Deployed");
        (await db.WmsLocations.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Reset_AfterDeploy_ShouldCancelOpenBusOrdersAndStackerTasks()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject("ResetDemo"));

        var legId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.BusTransportOrders.Add(new BusTransportOrder
        {
            Id = orderId,
            ContainerCode = "C1",
            FromLocationCode = "Stk.RECV-01",
            ToLocationCode = "Stk.BIN-01",
            Status = BusOrderStatus.Executing,
            CreateDate = DateTime.UtcNow
        });
        db.BusTransportLegs.Add(new BusTransportLeg
        {
            Id = legId,
            OrderId = orderId,
            PackId = "stacker",
            Seq = 1,
            FromCode = "Stk.RECV-01",
            ToCode = "Stk.BIN-01",
            ContainerCode = "C1",
            Status = BusLegStatus.Running,
            CreateDate = DateTime.UtcNow
        });
        db.StkPutAwayTasks.Add(new StkPutAwayTask
        {
            Id = Guid.NewGuid(),
            LegId = legId,
            ContainerCode = "C1",
            FromCode = "Stk.RECV-01",
            ToCode = "Stk.BIN-01",
            Status = StkPutAwayStatus.LocationAssigned,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await svc.ResetAsync("ResetDemo");

        (await db.SimDeployments.SingleAsync()).Status.Should().Be("Deployed");
        (await db.WmsLocations.CountAsync()).Should().Be(2);
        (await db.BusTransportOrders.SingleAsync()).Status.Should().Be(BusOrderStatus.Failed);
        (await db.BusTransportLegs.SingleAsync()).Status.Should().Be(BusLegStatus.Cancelled);
        (await db.StkPutAwayTasks.SingleAsync()).Status.Should().Be(StkPutAwayStatus.Cancelled);
    }

    private static SimPromoteRequest PromoteRequest(
        string projectName = "Demo",
        params SimPromoteDeviceDto[] devices) =>
        new(projectName, devices);

    private static SimPromoteDeviceDto RealDevice(
        string code = "STK-01",
        string host = "192.168.1.10",
        int port = 102,
        string protocol = "Step7") =>
        new(code, host, port, protocol);

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("::1")]
    public async Task PromotePreview_ShouldRejectLoopbackHost(string host)
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject());

        var act = () => svc.PromotePreviewAsync(PromoteRequest(devices: RealDevice(host: host)));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*环回*");
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("localhost")]
    [InlineData("::1")]
    public async Task Promote_ShouldRejectLoopbackHost(string host)
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject());

        var act = () => svc.PromoteAsync(PromoteRequest(devices: RealDevice(host: host)));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*环回*");
    }

    [Fact]
    public async Task PromotePreview_ShouldReturnDevicesAndWarnings()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject("PreviewDemo"));

        var devices = new[]
        {
            RealDevice("STK-01", "192.168.1.10", 102, "Step7"),
            RealDevice("STK-02", "192.168.1.11", 502, "ModbusTcp")
        };
        var result = await svc.PromotePreviewAsync(PromoteRequest("PreviewDemo", devices));

        result.ProjectName.Should().Be("PreviewDemo");
        result.Devices.Should().HaveCount(2);
        result.Devices[0].Code.Should().Be("STK-01");
        result.Devices[0].Host.Should().Be("192.168.1.10");
        result.Devices[1].Code.Should().Be("STK-02");
        result.Warnings.Should().NotBeNull();
        (await db.SimDeployments.SingleAsync()).Status.Should().Be("Deployed");
    }

    [Fact]
    public async Task Promote_ShouldMarkDeploymentPromoted_AndUpsertCommConnection()
    {
        var db = CreateDb();
        var svc = new SimulationDeployService(db);
        await svc.DeployAsync(SampleProject("PromoteDemo"));

        var devices = new[] { RealDevice("STK-01", "192.168.1.20", 102, "Step7") };
        var result = await svc.PromoteAsync(PromoteRequest("PromoteDemo", devices));

        result.Status.Should().Be("Promoted");
        result.WarehouseCode.Should().Be("SIM_PROMOTEDEMO");
        result.CommConnectionCount.Should().Be(1);

        var deployment = await db.SimDeployments.SingleAsync();
        deployment.Status.Should().Be("Promoted");
        deployment.WarehouseCode.Should().Be("SIM_PROMOTEDEMO");

        var wh = await db.WmsWarehouses.SingleAsync();
        wh.Code.Should().Be("SIM_PROMOTEDEMO");

        var conn = await db.CommConnections.SingleAsync();
        conn.Name.Should().Be("STK-01");
        conn.Host.Should().Be("192.168.1.20");
        conn.Port.Should().Be(102);
    }
}
