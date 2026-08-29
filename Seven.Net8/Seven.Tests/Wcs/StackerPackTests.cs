using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

public class StackerPackTests
{
    [Fact]
    public async Task SelectAisle_ShouldRotate_AndSkipUnavailable()
    {
        var db = CreateDb();
        SeedPolicies(db,
            ("A1", available: true, maxHeight: 3, maxWeight: 100, dest: "EP-A1"),
            ("A2", available: true, maxHeight: 3, maxWeight: 100, dest: "EP-A2"),
            ("A3", available: false, maxHeight: 3, maxWeight: 100, dest: "EP-A3"));
        await db.SaveChangesAsync();

        var allocator = new StackerAisleAllocator(db);

        var first = await allocator.SelectAisleAsync(height: 1, weight: 10);
        var second = await allocator.SelectAisleAsync(height: 1, weight: 10);
        var third = await allocator.SelectAisleAsync(height: 1, weight: 10);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.AisleCode.Should().BeOneOf("A1", "A2");
        second!.AisleCode.Should().BeOneOf("A1", "A2");
        first.AisleCode.Should().NotBe(second.AisleCode);
        third!.AisleCode.Should().Be(first.AisleCode);

        new[] { first.AisleCode, second.AisleCode, third.AisleCode }.Should().NotContain("A3");
    }

    [Fact]
    public async Task SimulateDestinationRequest_ShouldDispatchDestination_OnPort()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        SeedStackerMaster(db);
        await db.SaveChangesAsync();

        var control = new ControlModeService(db);
        var pack = new StackerWcsPack(db, control);
        var dest = new StackerDestinationService(
            db,
            port,
            new StackerAisleAllocator(db),
            new StackerLocationAllocator(db),
            bus: null);
        dest.Subscribe();

        var leg = NewLeg("TP-DEST", "RECV-01", "LOC-A1-01");
        var accepted = await pack.AcceptLegAsync(leg);
        accepted.Accepted.Should().BeTrue();

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-DEST", "RP_IN_01", 1, 10, "OK"));

        port.DispatchedDestinations.Should().ContainSingle();
        var cmd = port.DispatchedDestinations.Single();
        cmd.ContainerCode.Should().Be("TP-DEST");
        cmd.DestinationPointCode.Should().Be("EP-A1");
        cmd.LegId.Should().Be(leg.LegId);
    }

    [Fact]
    public async Task AcceptLeg_ThenSimulatedComplete_ShouldRaiseOnLegEventCompleted()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        SeedStackerMaster(db);
        await db.SaveChangesAsync();

        var control = new ControlModeService(db);
        var pack = new StackerWcsPack(db, control);
        var completion = new RecordingCompletionHandler();
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        var dest = new StackerDestinationService(
            db,
            port,
            new StackerAisleAllocator(db),
            new StackerLocationAllocator(db),
            bus);
        dest.Subscribe();

        var orderId = await bus.CreateTransportOrderAsync(
            new CreateTransportOrderRequest("TP-E2E", "RECV-01", "LOC-A1-01"));

        var afterAccept = await LoadOrderAsync(db, orderId);
        afterAccept.Status.Should().Be(BusOrderStatus.Executing);
        var leg = afterAccept.Legs.Single();
        leg.Status.Should().Be(BusLegStatus.Accepted);
        pack.PackId.Should().Be("stacker");

        var putaway = await db.StkPutAwayTasks.SingleAsync(x => x.LegId == leg.Id);
        putaway.Status.Should().Be(StkPutAwayStatus.Accepted);

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-E2E", "RP_IN_01", 1, 10, "OK"));
        port.DispatchedDestinations.Should().ContainSingle(x => x.ContainerCode == "TP-E2E");

        await port.SimulateSegmentFeedbackAsync(new DeviceSegmentFeedback(
            "TP-E2E", "EP-A1", "OK", leg.Id));

        var afterDone = await LoadOrderAsync(db, orderId);
        afterDone.Status.Should().Be(BusOrderStatus.Completed);
        afterDone.Legs.Single().Status.Should().Be(BusLegStatus.Completed);
        completion.CompletedOrderIds.Should().Equal(orderId);

        var device = await db.StkDeviceTasks.SingleAsync(x => x.LegId == leg.Id);
        device.Status.Should().Be(StkDeviceTaskStatus.Completed);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Stk_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void SeedPolicies(
        SevenDbContext db,
        params (string Aisle, bool available, int maxHeight, int maxWeight, string dest)[] rows)
    {
        foreach (var row in rows)
        {
            db.StkAssignmentPolicies.Add(new StkAssignmentPolicy
            {
                AisleCode = row.Aisle,
                IsAvailable = row.available,
                MaxHeight = row.maxHeight,
                MaxWeight = row.maxWeight,
                DestinationPointCode = row.dest
            });
        }
    }

    private static void SeedStackerMaster(SevenDbContext db)
    {
        SeedPolicies(db, ("A1", true, 3, 100, "EP-A1"));
        db.StkRequestPoints.Add(new StkRequestPoint
        {
            Code = "RP_IN_01",
            PointType = StkRequestPointType.AisleRequest,
            IsEnabled = true
        });
        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = 1,
            Code = "LOC-A1-01",
            Aisle = "A1"
        });
    }

    private static TransportLegDto NewLeg(string container, string from, string to)
        => new(Guid.NewGuid(), Guid.NewGuid(), "stacker", 1, from, to, container, null, null);

    private static Task<BusTransportOrder> LoadOrderAsync(SevenDbContext db, Guid orderId)
        => db.BusTransportOrders
            .Include(x => x.Legs)
            .AsNoTracking()
            .SingleAsync(x => x.Id == orderId);

    private sealed class RecordingCompletionHandler : IWmsTransportCompletionHandler
    {
        public List<Guid> CompletedOrderIds { get; } = [];

        public Task OnTransportCompletedAsync(Guid orderId, CancellationToken ct = default)
        {
            CompletedOrderIds.Add(orderId);
            return Task.CompletedTask;
        }
    }
}
