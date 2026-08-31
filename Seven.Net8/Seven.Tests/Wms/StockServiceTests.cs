using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wms;

public class StockServiceTests
{
    private static async Task<(SevenDbContext Db, IStockService Stock)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"WmsStock_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        var warehouse = new WmsWarehouse { Code = "WH1", Name = "主仓" };
        db.WmsWarehouses.Add(warehouse);
        await db.SaveChangesAsync();

        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = warehouse.Id,
            Code = "LOC-A",
            IsOccupied = false
        });
        await db.SaveChangesAsync();

        return (db, new StockService(db));
    }

    [Fact]
    public async Task ReceiveAsync_ShouldIncreaseQty_AndOccupyLocation()
    {
        var (db, stock) = await CreateAsync();

        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m));

        var row = await db.WmsStocks.SingleAsync();
        row.Qty.Should().Be(10m);
        row.AvailableQty.Should().Be(10m);
        row.MaterialCode.Should().Be("MAT-01");
        row.LocationCode.Should().Be("LOC-A");

        var loc = await db.WmsLocations.SingleAsync(x => x.Code == "LOC-A");
        loc.IsOccupied.Should().BeTrue();

        var ledger = await db.WmsStockLedgers.SingleAsync();
        ledger.DeltaQty.Should().Be(10m);
        ledger.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ShipAsync_WhenInsufficient_ShouldThrow()
    {
        var (_, stock) = await CreateAsync();

        var act = async () => await stock.ShipAsync(new ShipStockRequest("LOC-A", "MAT-01", 1m));

        await act.Should().ThrowAsync<WmsDomainException>();
    }

    [Fact]
    public async Task ReceiveThenShip_ShouldConserveQuantity()
    {
        var (db, stock) = await CreateAsync();

        await stock.ReceiveAsync(new ReceiveStockRequest("LOC-A", "MAT-01", 10m));
        await stock.ShipAsync(new ShipStockRequest("LOC-A", "MAT-01", 10m));

        var row = await db.WmsStocks.SingleAsync();
        row.Qty.Should().Be(0m);
        row.AvailableQty.Should().Be(0m);

        var ledgers = await db.WmsStockLedgers.OrderBy(x => x.Id).ToListAsync();
        ledgers.Should().HaveCount(2);
        ledgers.Sum(x => x.DeltaQty).Should().Be(0m);
    }
}
