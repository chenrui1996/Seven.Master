using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wms;

public class PickingServiceTests
{
    [Fact]
    public async Task Approve_GeneratePicks_Confirm_ShouldCompleteWithoutTransport()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Pick_{Guid.NewGuid():N}")
            .Options;
        await using var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        db.WmsWarehouses.Add(new WmsWarehouse { Code = "WH1", Name = "主仓" });
        await db.SaveChangesAsync();
        var wh = db.WmsWarehouses.Single().Id;
        db.WmsLocations.Add(new WmsLocation { WarehouseId = wh, Code = "LOC-A" });
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m));

        var picking = new PickingService(db, stock);
        var outbound = new OutboundOrderService(db, stock, picking);
        var order = await outbound.CreateAsync(new CreateOutboundOrderRequest(
            "OUT-PICK-1",
            WmsOrderType.Other,
            [new OutboundLineInput(1, "MAT-01", 10m, FromLocation: "LOC-A")]));

        await outbound.ApproveAsync(order.Id);

        var task = await db.WmsPickingTasks.SingleAsync();
        task.Status.Should().Be(WmsPickingTaskStatus.Booked);
        task.BookQty.Should().Be(10m);
        (await db.WmsStocks.SingleAsync()).AvailableQty.Should().Be(0m);

        await picking.ConfirmPickAsync(new ConfirmPickRequest(task.Id));

        (await db.WmsPickingTasks.SingleAsync()).Status.Should().Be(WmsPickingTaskStatus.Completed);
        (await db.WmsOutboundOrders.SingleAsync()).Status.Should().Be(WmsOrderStatus.Completed);
        (await db.WmsStocks.SingleAsync()).Qty.Should().Be(0m);
    }
}
