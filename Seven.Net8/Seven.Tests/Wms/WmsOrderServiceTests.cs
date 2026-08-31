using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wms;

public class WmsOrderServiceTests
{
    private static async Task<(
        SevenDbContext Db,
        IStockService Stock,
        IInboundOrderService Inbound,
        IOutboundOrderService Outbound,
        ICycleCountService CycleCount)> CreateAsync(ITransportOrderRequest? transport = null)
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"WmsOrders_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        var warehouse = new WmsWarehouse { Code = "WH1", Name = "主仓" };
        db.WmsWarehouses.Add(warehouse);
        await db.SaveChangesAsync();

        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = warehouse.Id, Code = "RECV-01" },
            new WmsLocation { WarehouseId = warehouse.Id, Code = "LOC-A" });
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        return (db, stock, new InboundOrderService(db, stock, transport), new OutboundOrderService(db, stock, new PickingService(db, stock, transport), transport),
            new CycleCountService(db, stock));
    }

    [Fact]
    public async Task Inbound_CreateApproveReceive_ShouldIncreaseStock()
    {
        var (db, _, inbound, _, _) = await CreateAsync();

        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-001",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-01", 10m, ContainerCode: "TP001", ToLocation: "RECV-01")]));

        await inbound.ApproveAsync(order.Id);
        await inbound.ReceiveAndBuildPalletAsync(order.Id);

        var saved = await db.WmsInboundOrders.Include(x => x.Lines).SingleAsync();
        saved.Status.Should().Be(WmsOrderStatus.Completed);
        saved.Lines.Single().CompletedQty.Should().Be(10m);

        var row = await db.WmsStocks.SingleAsync();
        row.Qty.Should().Be(10m);
        row.AvailableQty.Should().Be(10m);
        row.MaterialCode.Should().Be("MAT-01");
        row.LocationCode.Should().Be("RECV-01");
        row.ContainerCode.Should().Be("TP001");
    }

    [Fact]
    public async Task Inbound_ReceiveWithTransport_ShouldStayAtReceiveLoc_AndStayExecuting()
    {
        var transport = new RecordingTransport();
        var (db, _, inbound, _, _) = await CreateAsync(transport);

        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-002",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-01", 10m, ContainerCode: "TP002", FromLocation: "RECV-01", ToLocation: "LOC-A")]));

        await inbound.ApproveAsync(order.Id);
        await inbound.ReceiveAndBuildPalletAsync(order.Id, new ReceiveAndBuildPalletRequest("RECV-01"));

        var saved = await db.WmsInboundOrders.Include(x => x.Lines).SingleAsync();
        saved.Status.Should().Be(WmsOrderStatus.Executing);

        var row = await db.WmsStocks.SingleAsync();
        row.LocationCode.Should().Be("RECV-01");
        row.Qty.Should().Be(10m);

        transport.Requests.Should().ContainSingle();
        var hook = transport.Requests.Single();
        hook.FromLocationCode.Should().Be("RECV-01");
        hook.ToLocationCode.Should().Be("LOC-A");
        hook.RefType.Should().Be("InboundDetail");
        (await db.WmsInboundDetails.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Outbound_ApproveThenShip_ShouldDecreaseStock()
    {
        var (db, stock, _, outbound, _) = await CreateAsync();
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m));

        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-001",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, FromLocation: "LOC-A")]));

        await outbound.ApproveAsync(order.Id);
        await outbound.ShipAsync(order.Id);

        var saved = await db.WmsOutboundOrders.Include(x => x.Lines).SingleAsync();
        saved.Status.Should().Be(WmsOrderStatus.Completed);
        saved.Lines.Single().CompletedQty.Should().Be(10m);

        var row = await db.WmsStocks.SingleAsync();
        row.Qty.Should().Be(0m);
        row.AvailableQty.Should().Be(0m);
    }

    [Fact]
    public async Task CycleCount_ConfirmAdjust_ShouldSetStockToCountQty()
    {
        var (db, stock, _, _, cycleCount) = await CreateAsync();
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m));

        var plan = await cycleCount.CreatePlanAsync(new CreateCycleCountRequest(
            "CC-001",
            [new CycleCountLineInput(1, "LOC-A", "MAT-01")]));
        plan.Lines.Single().BookQty.Should().Be(10m);

        await cycleCount.RecordCountAsync(plan.Id, lineNo: 1, countQty: 8m);
        await cycleCount.ConfirmAdjustAsync(plan.Id);

        var saved = await db.WmsCycleCounts.Include(x => x.Lines).SingleAsync();
        saved.Status.Should().Be(WmsOrderStatus.Completed);
        saved.Lines.Single().CountQty.Should().Be(8m);
        saved.Lines.Single().DiffQty.Should().Be(-2m);

        var row = await db.WmsStocks.SingleAsync();
        row.Qty.Should().Be(8m);
        row.AvailableQty.Should().Be(8m);
    }

    [Fact]
    public async Task Outbound_ApproveWithToLocation_ShouldRequestTransport_AndReserve_WithoutShipping()
    {
        var transport = new RecordingTransport();
        var (db, stock, _, outbound, _) = await CreateAsync(transport);
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m, ContainerCode: "TP-OUT"));

        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-002",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, FromLocation: "LOC-A", ToLocation: "DOCK-01", ContainerCode: "TP-OUT")]));

        await outbound.ApproveAsync(order.Id);

        var saved = await db.WmsOutboundOrders.SingleAsync();
        saved.Status.Should().Be(WmsOrderStatus.Executing);

        var row = await db.WmsStocks.SingleAsync();
        row.Qty.Should().Be(10m);
        row.AvailableQty.Should().Be(0m);
        row.LocationCode.Should().Be("LOC-A");
        transport.Requests.Should().BeEmpty();
        (await db.WmsPickingTasks.CountAsync()).Should().Be(1);

        var picking = new PickingService(db, stock, transport);
        await picking.ConfirmPickAsync(new ConfirmPickRequest((await db.WmsPickingTasks.SingleAsync()).Id));

        transport.Requests.Should().ContainSingle();
        var hook = transport.Requests.Single();
        hook.FromLocationCode.Should().Be("LOC-A");
        hook.ToLocationCode.Should().Be("DOCK-01");
        hook.RefType.Should().Be("OutboundOrder");
        hook.RefId.Should().Be("OUT-002");

        var act = () => outbound.ShipAsync(order.Id);
        await act.Should().ThrowAsync<WmsDomainException>()
            .WithMessage("*运输*");
    }

    [Fact]
    public async Task Outbound_TransportCompleted_ShouldMoveStockAndCompleteOrder()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"WmsOutComplete_{Guid.NewGuid():N}")
            .Options;
        await using var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        db.WmsWarehouses.Add(new WmsWarehouse { Code = "WH1", Name = "主仓" });
        await db.SaveChangesAsync();
        var whId = db.WmsWarehouses.Single().Id;
        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = whId, Code = "LOC-A" },
            new WmsLocation { WarehouseId = whId, Code = "DOCK-01" });
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m, ContainerCode: "TP-OUT"));

        var transport = new RecordingTransport();
        var outbound = new OutboundOrderService(db, stock, new PickingService(db, stock, transport), transport);
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-003",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, FromLocation: "LOC-A", ToLocation: "DOCK-01", ContainerCode: "TP-OUT")]));
        await outbound.ApproveAsync(order.Id);
        await new PickingService(db, stock, transport).ConfirmPickAsync(
            new ConfirmPickRequest((await db.WmsPickingTasks.SingleAsync()).Id));

        var busOrderId = Guid.NewGuid();
        db.BusTransportOrders.Add(new BusTransportOrder
        {
            Id = busOrderId,
            FromLocationCode = "LOC-A",
            ToLocationCode = "DOCK-01",
            ContainerCode = "TP-OUT",
            RefType = "OutboundOrder",
            RefId = "OUT-003",
            Status = BusOrderStatus.Completed,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new WmsTransportCompletionHandler(db, stock);
        await handler.OnTransportCompletedAsync(busOrderId);

        (await db.WmsStocks.SingleAsync(s => s.LocationCode == "DOCK-01")).Qty.Should().Be(10m);
        (await db.WmsStocks.Where(s => s.LocationCode == "LOC-A" && s.Qty > 0).AnyAsync()).Should().BeFalse();
        (await db.WmsOutboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
    }

    private sealed class RecordingTransport : ITransportOrderRequest
    {
        public bool IsEnabled => true;
        public List<TransportOrderHookRequest> Requests { get; } = [];

        public Task<Guid> RequestAsync(TransportOrderHookRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(Guid.NewGuid());
        }
    }
}
