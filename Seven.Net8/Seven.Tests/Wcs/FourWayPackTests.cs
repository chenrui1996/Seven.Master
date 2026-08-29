using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Packs.FourWay;

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
    public async Task AcceptLeg_ShouldCreateShuttleTask_WhenCanHandle()
    {
        var db = CreateDb();
        var pack = new FourWayWcsPack(db, new ControlModeService(db));

        pack.PackId.Should().Be("fourway");
        (await pack.CanHandleAsync("FW-A", "FW-B")).Should().BeTrue();

        var leg = new TransportLegDto(
            Guid.NewGuid(), Guid.NewGuid(), "fourway", 1, "FW-A", "FW-B", "TP-FW-1", null, null);

        var result = await pack.AcceptLegAsync(leg);

        result.Accepted.Should().BeTrue();
        var task = await db.FwShuttleTasks.SingleAsync(x => x.LegId == leg.LegId);
        task.ContainerCode.Should().Be("TP-FW-1");
        task.FromCode.Should().Be("FW-A");
        task.ToCode.Should().Be("FW-B");
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
}
