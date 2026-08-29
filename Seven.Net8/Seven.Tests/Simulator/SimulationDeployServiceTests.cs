using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Simulator;
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
    public void ValidateFeatures_ShouldFail_WhenWmsOff()
    {
        var svc = new SimulationDeployService(CreateDb());
        var result = svc.ValidateFeatures(new SimFeaturesDto(false, false, false, false, true, new SimWcsPacksDto(true, false, false)));
        result.Ok.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Wms"));
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
        (await db.WmsLocations.CountAsync()).Should().Be(2);
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
}
