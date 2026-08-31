using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Business;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Business;

public class WmsExtensionHooksTests
{
    private sealed class EvenPriOnlyHooks : IWmsExtensionHooks
    {
        public Task BeforeInboundApproveAsync(WmsInboundOrder order, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task AfterInboundApproveAsync(WmsInboundOrder order, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task BeforeOutboundApproveAsync(WmsOutboundOrder order, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task AfterOutboundApproveAsync(WmsOutboundOrder order, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task<IReadOnlyList<WmsPickingTask>> FilterPendingPickingAsync(
            IReadOnlyList<WmsPickingTask> tasks,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WmsPickingTask>>(
                tasks.Where(t => t.WcsPri % 2 == 0).ToList());
        public Task<IReadOnlyList<int>> FilterPendingInboundOrderIdsAsync(
            IReadOnlyList<int> orderIds,
            CancellationToken ct = default) =>
            Task.FromResult(orderIds);
    }

    [Fact]
    public async Task ListPending_ShouldApplyFilterHook()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Hooks_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        var order = new WmsOutboundOrder
        {
            OrderNo = "OUT-H1",
            Status = WmsOrderStatus.Approved,
            OrderType = WmsOrderType.Other
        };
        db.WmsOutboundOrders.Add(order);
        await db.SaveChangesAsync();

        db.WmsPickingTasks.AddRange(
            new WmsPickingTask
            {
                TaskNo = "P-1",
                OutboundOrderId = order.Id,
                LineId = 1,
                MaterialCode = "M",
                BookQty = 1,
                PickQty = 0,
                WcsPri = 1,
                Status = WmsPickingTaskStatus.Booked
            },
            new WmsPickingTask
            {
                TaskNo = "P-2",
                OutboundOrderId = order.Id,
                LineId = 2,
                MaterialCode = "M",
                BookQty = 1,
                PickQty = 0,
                WcsPri = 2,
                Status = WmsPickingTaskStatus.Booked
            });
        await db.SaveChangesAsync();

        var stock = new StockService(db);
        var picking = new PickingService(db, stock, hooks: new EvenPriOnlyHooks());
        var pending = await picking.ListPendingAsync();
        pending.Should().ContainSingle(t => t.TaskNo == "P-2");
    }
}
