using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Packs.Stacker;

namespace Seven.Tests.Platform;

public class PlatformIfcCtlTests
{
    [Fact]
    public async Task WriteLog_ThenCount_ShouldPersistEntry()
    {
        var db = CreateDb();
        var logService = new InterfaceLogService(db);

        await logService.WriteAsync(new InterfaceLogWriteRequest(
            InterfaceLogDirection.Out,
            "WMS",
            DurationMs: 42,
            Success: true,
            CorrelationId: "corr-1",
            LegId: Guid.NewGuid(),
            OrderNo: "ORD-001",
            Path: "/api/test",
            RequestBody: "{\"a\":1}",
            ResponseBody: "{\"ok\":true}"));

        var count = await logService.CountAsync();
        count.Should().Be(1);

        var row = await db.IfcApiLogs.SingleAsync();
        row.SystemCode.Should().Be("WMS");
        row.Direction.Should().Be(InterfaceLogDirection.Out);
        row.DurationMs.Should().Be(42);
        row.Success.Should().BeTrue();
        row.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public async Task EStopTrue_StackerAcceptLeg_ShouldReject()
    {
        var db = CreateDb();
        var control = new ControlModeService(db);
        await control.SetEStopAsync("stacker", eStop: true);
        var pack = new StackerWcsPack(db, control);

        var leg = new TransportLegDto(
            Guid.NewGuid(), Guid.NewGuid(), "stacker", 1, "RECV-01", "LOC-A1-01", "TP-1", null, null);

        var result = await pack.AcceptLegAsync(leg);

        result.Accepted.Should().BeFalse();
        result.RejectReason.Should().NotBeNullOrWhiteSpace();
        (await db.StkPutAwayTasks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ManualMode_StackerAcceptLeg_ShouldReject()
    {
        var db = CreateDb();
        var control = new ControlModeService(db);
        await control.SetModeAsync("stacker", WcsControlMode.Manual);
        var pack = new StackerWcsPack(db, control);

        var leg = new TransportLegDto(
            Guid.NewGuid(), Guid.NewGuid(), "stacker", 1, "RECV-01", "LOC-A1-01", "TP-1", null, null);

        var result = await pack.AcceptLegAsync(leg);

        result.Accepted.Should().BeFalse();
        (await db.StkPutAwayTasks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GlobalEStop_StackerHealth_ShouldNotAcceptLegs()
    {
        var db = CreateDb();
        var control = new ControlModeService(db);
        await control.SetEStopAsync("Global", eStop: true);
        var pack = new StackerWcsPack(db, control);

        var health = await pack.HealthAsync();

        health.CanAcceptLegs.Should().BeFalse();
        health.IsHealthy.Should().BeFalse();
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Platform_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
