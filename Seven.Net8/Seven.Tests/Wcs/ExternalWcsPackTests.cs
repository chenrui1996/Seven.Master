using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.External;

namespace Seven.Tests.Wcs;

public class ExternalWcsPackTests
{
    [Fact]
    public async Task AcceptLeg_ShouldPostEncodedBody_AndLogOutbound()
    {
        string? capturedBody = null;
        var handler = new CapturingHttpHandler(async req =>
        {
            capturedBody = req.Content == null
                ? null
                : await req.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var transport = new HttpExternalTransport(new HttpClient(handler));
        var codec = new FakeVendorCodec();
        var db = CreateDb();
        var options = new ExternalWcsEntryOptions
        {
            PackId = "ext:test",
            Transport = "Http",
            Codec = "Fake",
            BaseUrl = "http://fake.local/tasks",
            Enabled = true
        };
        var pack = new ExternalWcsPack(options, transport, codec, db);

        var legId = Guid.NewGuid();
        var leg = new TransportLegDto(
            legId,
            Guid.NewGuid(),
            "ext:test",
            1,
            "RECV-01",
            "LOC-A1",
            "TP-EXT-01",
            null,
            null);

        var result = await pack.AcceptLegAsync(leg);

        result.Accepted.Should().BeTrue();
        capturedBody.Should().NotBeNullOrWhiteSpace();
        capturedBody.Should().Contain(legId.ToString());
        capturedBody.Should().Contain("RECV-01");

        var log = await db.ExtMessageLogs.SingleAsync();
        log.SystemPackId.Should().Be("ext:test");
        log.Direction.Should().Be(InterfaceLogDirection.Out);
        log.Success.Should().BeTrue();
        log.CorrelationId.Should().Be(legId.ToString("N"));
    }

    [Fact]
    public async Task HandleCallback_Completed_ShouldCompleteOrderOnBus()
    {
        var db = CreateDb();
        var codec = new FakeVendorCodec();
        var transport = new HttpExternalTransport(new HttpClient(new CapturingHttpHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))));
        var completion = new RecordingCompletionHandler();
        var options = new ExternalWcsEntryOptions
        {
            PackId = "ext:agv:vendorA",
            Transport = "Http",
            Codec = "Fake",
            BaseUrl = "http://fake.local/tasks",
            Enabled = true
        };

        var pack = new ExternalWcsPack(options, transport, codec, db);
        var bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);
        pack = new ExternalWcsPack(options, transport, codec, db, bus);
        bus = new OrchestrationBus(db, new WcsPackResolver([pack]), completion);

        var orderId = await bus.CreateTransportOrderAsync(
            new CreateTransportOrderRequest("TP-CB", "RECV-01", "LOC-A1"));

        var order = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync(x => x.Id == orderId);
        var leg = order.Legs.Single();
        leg.Status.Should().Be(BusLegStatus.Accepted);
        completion.CompletedOrderIds.Should().BeEmpty();

        var callbackPayload = FakeVendorCodec.EncodeCallback(leg.Id, LegEventType.Completed);
        await pack.HandleCallbackAsync(callbackPayload);

        var afterDone = await db.BusTransportOrders.Include(x => x.Legs).SingleAsync(x => x.Id == orderId);
        afterDone.Status.Should().Be(BusOrderStatus.Completed);
        afterDone.Legs.Single().Status.Should().Be(BusLegStatus.Completed);
        completion.CompletedOrderIds.Should().Equal(orderId);

        var inboundLog = await db.ExtMessageLogs
            .Where(x => x.Direction == InterfaceLogDirection.In)
            .SingleAsync();
        inboundLog.Success.Should().BeTrue();
        inboundLog.SystemPackId.Should().Be("ext:agv:vendorA");
    }

    [Fact]
    public void MqTransport_WhenMessageQueueDisabled_ShouldThrowNotSupported()
    {
        var transport = new MqExternalTransport(Microsoft.Extensions.Options.Options.Create(new FeatureOptions()));
        var act = () => transport.SendAsync(
            new ExternalSendRequest("ext:mq", null, "{}", "corr"),
            CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*MessageQueue*");
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Ext_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class CapturingHttpHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public CapturingHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _handler(request);
    }

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
