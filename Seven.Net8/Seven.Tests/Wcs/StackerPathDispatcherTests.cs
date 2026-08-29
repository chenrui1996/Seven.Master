using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

public class StackerPathDispatcherTests
{
    [Fact]
    public async Task Dispatch_ShouldMergeSameExeStack_AndAdvanceSegments()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        db.StkRoutes.AddRange(
            new StkRoute { FromCode = "RP", ToCode = "M1", ExeStackCode = "CV", Weight = 1, Capacity = 2, MapCode = "" },
            new StkRoute { FromCode = "M1", ToCode = "EP", ExeStackCode = "CV", Weight = 1, Capacity = 2, MapCode = "" },
            new StkRoute { FromCode = "EP", ToCode = "BIN", ExeStackCode = "SRM", Weight = 1, Capacity = 1, MapCode = "" });
        db.StkDeviceCoders.Add(new StkDeviceCoder { LocationCode = "Stk.B-01", PointCode = "BIN" });
        await db.SaveChangesAsync();

        var dispatcher = new StackerPathDispatcher(db, port);
        var legId = Guid.NewGuid();
        var putAwayId = Guid.NewGuid();
        var tasks = await dispatcher.DispatchAsync(new StackerPathDispatchRequest(
            "RP", "Stk.B-01", "TP-R", legId, putAwayId, null));

        // JudgeMap BIN + merge CV: RP→EP (CV), EP→BIN (SRM)
        tasks.Should().HaveCount(2);
        tasks[0].ExeStackCode.Should().Be("CV");
        tasks[0].DestinationPointCode.Should().Be("EP");
        tasks[0].Status.Should().Be(StkDeviceTaskStatus.Dispatched);
        tasks[1].ExeStackCode.Should().Be("SRM");
        tasks[1].Status.Should().Be(StkDeviceTaskStatus.Created);
        port.DispatchedDestinations.Should().ContainSingle(x => x.DestinationPointCode == "EP");
        (await db.StkRouteFlows.CountAsync()).Should().Be(3);

        tasks[0].Status = StkDeviceTaskStatus.Completed;
        await db.SaveChangesAsync();
        var advanced = await dispatcher.AdvanceAfterSegmentAsync(tasks[0]);
        advanced.Should().BeTrue();
        (await db.StkDeviceTasks.SingleAsync(x => x.Seq == 2)).Status.Should().Be(StkDeviceTaskStatus.Dispatched);
        port.DispatchedDestinations.Should().Contain(x => x.DestinationPointCode == "BIN");

        var last = await db.StkDeviceTasks.SingleAsync(x => x.Seq == 2);
        last.Status = StkDeviceTaskStatus.Completed;
        await db.SaveChangesAsync();
        (await dispatcher.AdvanceAfterSegmentAsync(last)).Should().BeFalse();
        (await db.StkRouteFlows.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Dispatch_WithoutRoutes_ShouldFallbackSingleSegment()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        var dispatcher = new StackerPathDispatcher(db, port);
        var tasks = await dispatcher.DispatchAsync(new StackerPathDispatchRequest(
            "A", "B", "TP", Guid.NewGuid(), Guid.NewGuid(), null));
        tasks.Should().ContainSingle();
        tasks[0].DestinationPointCode.Should().Be("B");
        port.DispatchedDestinations.Should().ContainSingle();
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"StkRoute_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
