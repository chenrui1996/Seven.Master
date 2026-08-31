using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Business;
using Seven.Application.Wms;
using Seven.Business;
using Seven.Domain.Business;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Business;

/// <summary>业务扩展样板：调拨单状态机 + 组合库存。</summary>
public class TransferOrderServiceTests
{
    private static async Task<(SevenDbContext Db, ITransferOrderService Svc, IStockService Stock)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"BizTransfer_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        var warehouse = new WmsWarehouse { Code = "WH1", Name = "主仓" };
        db.WmsWarehouses.Add(warehouse);
        await db.SaveChangesAsync();

        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = warehouse.Id, Code = "LOC-A", IsOccupied = false },
            new WmsLocation { WarehouseId = warehouse.Id, Code = "LOC-B", IsOccupied = false });
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        var svc = new TransferOrderService(db, stock);
        return (db, svc, stock);
    }

    [Fact]
    public async Task Approve_Then_Complete_ShouldMoveStock()
    {
        var (db, svc, stock) = await CreateAsync();
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m));

        var order = await svc.CreateAsync(new CreateTransferOrderRequest(
            "TR-001",
            new[] { new TransferLineInput(1, "MAT-01", 4m, "LOC-A", "LOC-B") }));

        await svc.ApproveAsync(order.Id, default);
        var approved = await db.TransferOrders.SingleAsync(x => x.Id == order.Id);
        approved.Status.Should().Be(WmsOrderStatus.Approved);

        var afterBook = await db.WmsStocks.SingleAsync(x => x.LocationCode == "LOC-A");
        afterBook.Qty.Should().Be(10m);
        afterBook.AvailableQty.Should().Be(6m);

        await svc.CompleteAsync(order.Id, default);
        var done = await db.TransferOrders.Include(x => x.Lines).SingleAsync(x => x.Id == order.Id);
        done.Status.Should().Be(WmsOrderStatus.Completed);
        done.Lines.Single().CompletedQty.Should().Be(4m);

        var from = await db.WmsStocks.SingleAsync(x => x.LocationCode == "LOC-A" && x.MaterialCode == "MAT-01");
        from.Qty.Should().Be(6m);
        var to = await db.WmsStocks.SingleAsync(x => x.LocationCode == "LOC-B" && x.MaterialCode == "MAT-01");
        to.Qty.Should().Be(4m);
    }

    [Fact]
    public async Task Approve_WhenNotDraft_ShouldThrow()
    {
        var (_, svc, stock) = await CreateAsync();
        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 5m));
        var order = await svc.CreateAsync(new CreateTransferOrderRequest(
            "TR-002",
            new[] { new TransferLineInput(1, "MAT-01", 1m, "LOC-A", "LOC-B") }));
        await svc.ApproveAsync(order.Id, default);

        var act = async () => await svc.ApproveAsync(order.Id, default);
        await act.Should().ThrowAsync<BizDomainException>();
    }
}
