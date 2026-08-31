using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Pda;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Pda;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Pda;

public class PdaServiceTests
{
    private static async Task<(SevenDbContext Db, IPdaService Pda, IInboundOrderService Inbound, ICycleCountService CycleCount, IStockService Stock)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Pda_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        var warehouse = new WmsWarehouse { Code = "WH1", Name = "主仓", EnabledPackIds = "stacker" };
        db.WmsWarehouses.Add(warehouse);
        await db.SaveChangesAsync();
        db.WmsLocations.AddRange(
            new WmsLocation { WarehouseId = warehouse.Id, PackId = "stacker", Code = "Stk.RECV-01" },
            new WmsLocation { WarehouseId = warehouse.Id, PackId = "stacker", Code = "Stk.FLOOR-01" });
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        var inbound = new InboundOrderService(db, stock, transport: null);
        var cycleCount = new CycleCountService(db, stock);
        var picking = new PickingService(db, stock, transport: null);
        var pda = new PdaService(
            db, inbound, stock, cycleCount, picking,
            menuContributors: [new Seven.Business.TransferOrderPdaMenuContributor()]);
        return (db, pda, inbound, cycleCount, stock);
    }

    [Fact]
    public async Task Menu_ShouldContainStandardItems_AndTransferExtension()
    {
        var (_, pda, _, _, _) = await CreateAsync();
        var codes = pda.GetMenu().Select(x => x.Code).ToList();
        codes.Should().Contain(["receive", "putaway", "picking", "cyclecount"]);
        codes.Should().Contain("transfer");
    }

    [Fact]
    public async Task FloorReceive_ThenPutaway_ShouldMoveStock()
    {
        var (db, pda, inbound, _, _) = await CreateAsync();
        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-PDA-001",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-01", 10m)]));
        await inbound.ApproveAsync(order.Id);

        var pending = await pda.GetPendingInboundAsync();
        pending.Should().ContainSingle(x => x.OrderNo == "IN-PDA-001");

        var recv = await pda.ReceiveFloorAsync(order.Id, new PdaReceiveRequest(
            1, 10m, "TP-PDA", "Stk.RECV-01"));
        recv.ReceiveLocationCode.Should().Be("Stk.RECV-01");
        recv.Status.Should().Be(WmsInboundDetailStatus.Completed);

        (await db.WmsStocks.SingleAsync(x => x.Qty > 0)).LocationCode.Should().Be("Stk.RECV-01");

        var putPending = await pda.GetPendingPutawayAsync();
        putPending.Should().ContainSingle(x => x.ContainerCode == "TP-PDA");

        var put = await pda.ConfirmPutawayAsync(new PdaPutawayConfirmRequest("TP-PDA", "Stk.FLOOR-01", recv.DetailId));
        put.FromLocationCode.Should().Be("Stk.RECV-01");
        put.ToLocationCode.Should().Be("Stk.FLOOR-01");

        (await db.WmsStocks.SingleAsync(x => x.Qty > 0)).LocationCode.Should().Be("Stk.FLOOR-01");
        (await db.WmsInboundDetails.SingleAsync()).TargetLocationCode.Should().Be("Stk.FLOOR-01");
        (await pda.GetPendingPutawayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CycleCount_RecordByLocation_ThenConfirm_ShouldAdjustStock()
    {
        var (db, pda, _, cycleCount, stock) = await CreateAsync();
        await stock.ReceiveAsync(new ReceiveStockRequest("Stk.FLOOR-01", "MAT-01", 10m));

        var plan = await cycleCount.CreatePlanAsync(new CreateCycleCountRequest(
            "CC-PDA-001",
            [new CycleCountLineInput(1, "Stk.FLOOR-01", "MAT-01")]));

        var pending = await pda.GetPendingCycleCountsAsync();
        pending.Should().ContainSingle(x => x.OrderNo == "CC-PDA-001" && x.TotalLines == 1 && x.CountedLines == 0);

        var recorded = await pda.RecordCycleCountAsync(plan.Id, new PdaCycleCountRecordRequest(
            LineNo: 0,
            CountQty: 8m,
            LocationCode: "Stk.FLOOR-01"));
        recorded.DiffQty.Should().Be(-2m);
        recorded.AllCounted.Should().BeTrue();

        await pda.ConfirmCycleCountAsync(plan.Id);

        (await db.WmsStocks.SingleAsync()).Qty.Should().Be(8m);
        (await pda.GetPendingCycleCountsAsync()).Should().BeEmpty();
    }
}
