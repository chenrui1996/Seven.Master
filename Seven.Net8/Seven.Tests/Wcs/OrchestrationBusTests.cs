using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Bus;

namespace Seven.Tests.Wcs;

public class OrchestrationBusTests
{
    [Fact]
    public async Task SinglePack_CreateThenComplete_ShouldCompleteOrder()
    {
        var pack = new FakePack("stacker", (_, _) => true);
        var completion = new RecordingCompletionHandler();
        var (db, bus) = CreateBus([pack], completion);

        var orderId = await bus.CreateTransportOrderAsync(
            new CreateTransportOrderRequest("TP001", "RECV-01", "LOC-A"));

        var afterCreate = await LoadOrderAsync(db, orderId);
        afterCreate.Status.Should().Be(BusOrderStatus.Executing);
        afterCreate.Legs.Should().HaveCount(1);
        var leg = afterCreate.Legs.Single();
        leg.Status.Should().Be(BusLegStatus.Accepted);
        pack.Accepted.Should().ContainSingle(x => x.LegId == leg.Id);
        completion.CompletedOrderIds.Should().BeEmpty();

        await bus.OnLegEventAsync(new LegEvent(leg.Id, LegEventType.Completed));

        var afterDone = await LoadOrderAsync(db, orderId);
        afterDone.Status.Should().Be(BusOrderStatus.Completed);
        afterDone.Legs.Single().Status.Should().Be(BusLegStatus.Completed);
        completion.CompletedOrderIds.Should().Equal(orderId);
    }

    [Fact]
    public async Task TwoPacks_WithHandover_FirstComplete_ShouldActivateSecond()
    {
        var packA = new FakePack("stacker", (from, to) => from == "RECV-01" && to == "HO-01");
        var packB = new FakePack("fourway", (from, to) => from == "HO-01" && to == "RACK-01");
        var (db, bus) = CreateBus([packA, packB]);
        db.WmsHandoverLinks.Add(new WmsHandoverLink
        {
            FromPackId = "stacker",
            ToPackId = "fourway",
            LocationCode = "HO-01"
        });
        await db.SaveChangesAsync();

        var orderId = await bus.CreateTransportOrderAsync(
            new CreateTransportOrderRequest("TP002", "RECV-01", "RACK-01"));

        var afterCreate = await LoadOrderAsync(db, orderId);
        afterCreate.Status.Should().Be(BusOrderStatus.Executing);
        afterCreate.Legs.Should().HaveCount(2);
        var leg1 = afterCreate.Legs.Single(x => x.Seq == 1);
        var leg2 = afterCreate.Legs.Single(x => x.Seq == 2);
        leg1.PackId.Should().Be("stacker");
        leg1.Status.Should().Be(BusLegStatus.Accepted);
        leg1.ToCode.Should().Be("HO-01");
        leg2.PackId.Should().Be("fourway");
        leg2.Status.Should().Be(BusLegStatus.Pending);
        packA.Accepted.Should().ContainSingle();
        packB.Accepted.Should().BeEmpty();

        await bus.OnLegEventAsync(new LegEvent(leg1.Id, LegEventType.Completed));

        var afterLeg1 = await LoadOrderAsync(db, orderId);
        afterLeg1.Status.Should().Be(BusOrderStatus.Executing);
        afterLeg1.Legs.Single(x => x.Seq == 1).Status.Should().Be(BusLegStatus.Completed);
        afterLeg1.Legs.Single(x => x.Seq == 2).Status.Should().Be(BusLegStatus.Accepted);
        packB.Accepted.Should().ContainSingle(x => x.FromCode == "HO-01" && x.ToCode == "RACK-01");
    }

    [Fact]
    public async Task LegFailed_ShouldStopSubsequentAndFailOrder()
    {
        var packA = new FakePack("stacker", (from, to) => from == "RECV-01" && to == "HO-01");
        var packB = new FakePack("fourway", (from, to) => from == "HO-01" && to == "RACK-01");
        var completion = new RecordingCompletionHandler();
        var (db, bus) = CreateBus([packA, packB], completion);
        db.WmsHandoverLinks.Add(new WmsHandoverLink
        {
            FromPackId = "stacker",
            ToPackId = "fourway",
            LocationCode = "HO-01"
        });
        await db.SaveChangesAsync();

        var orderId = await bus.CreateTransportOrderAsync(
            new CreateTransportOrderRequest("TP003", "RECV-01", "RACK-01"));
        var afterCreate = await LoadOrderAsync(db, orderId);
        var leg1 = afterCreate.Legs.Single(x => x.Seq == 1);

        await bus.OnLegEventAsync(new LegEvent(leg1.Id, LegEventType.Failed, Message: "device fault"));

        var afterFail = await LoadOrderAsync(db, orderId);
        afterFail.Status.Should().Be(BusOrderStatus.Failed);
        afterFail.Legs.Single(x => x.Seq == 1).Status.Should().Be(BusLegStatus.Failed);
        afterFail.Legs.Single(x => x.Seq == 2).Status.Should().Be(BusLegStatus.Pending);
        packB.Accepted.Should().BeEmpty();
        completion.CompletedOrderIds.Should().BeEmpty();
    }

    private static (SevenDbContext Db, OrchestrationBus Bus) CreateBus(
        IWcsPack[] packs,
        IWmsTransportCompletionHandler? completion = null)
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Bus_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        var resolver = new WcsPackResolver(packs);
        var bus = new OrchestrationBus(db, resolver, completion ?? new RecordingCompletionHandler());
        return (db, bus);
    }

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

    private sealed class FakePack : IWcsPack
    {
        private readonly Func<string, string, bool> _canHandle;

        public FakePack(string packId, Func<string, string, bool> canHandle)
        {
            PackId = packId;
            _canHandle = canHandle;
        }

        public string PackId { get; }
        public List<TransportLegDto> Accepted { get; } = [];

        public Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default)
            => Task.FromResult(_canHandle(fromLocationCode, toLocationCode));

        public Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default)
        {
            Accepted.Add(leg);
            return Task.FromResult(new AcceptLegResult(true));
        }

        public Task CancelLegAsync(Guid legId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default)
            => Task.FromResult<LegStatusDto?>(null);

        public Task<PackHealthDto> HealthAsync(CancellationToken ct = default)
            => Task.FromResult(new PackHealthDto(PackId, true, true));
    }
}
