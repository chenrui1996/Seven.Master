using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seven.Application.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wms;

/// <summary>运输单完成后：移库（收货位→目标 / 货位→出库口），并回写组盘明细与入/出库单。</summary>
public sealed class WmsTransportCompletionHandler : IWmsTransportCompletionHandler
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;
    private readonly ILogger<WmsTransportCompletionHandler>? _logger;

    public WmsTransportCompletionHandler(
        SevenDbContext db,
        IStockService stock,
        ILogger<WmsTransportCompletionHandler>? logger = null)
    {
        _db = db;
        _stock = stock;
        _logger = logger;
    }

    public async Task OnTransportCompletedAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.BusTransportOrders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
        {
            _logger?.LogWarning("TransportOrder {OrderId} not found for WMS completion", orderId);
            return;
        }

        await MoveStockIfNeededAsync(order.FromLocationCode, order.ToLocationCode, order.ContainerCode, order.RefType, order.RefId, ct);
        await OccupyTargetLocationAsync(order.ToLocationCode, order.ContainerCode, ct);
        if (string.Equals(order.RefType, "OutboundOrder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(order.RefType, "StackerTransfer", StringComparison.OrdinalIgnoreCase))
            await ReleaseSourceLocationAsync(order.FromLocationCode, ct);

        if (string.IsNullOrWhiteSpace(order.RefId))
            return;

        if (string.Equals(order.RefType, "InboundDetail", StringComparison.OrdinalIgnoreCase))
        {
            await CompleteInboundDetailAsync(order.RefId, orderId, ct);
            return;
        }

        if (string.Equals(order.RefType, "InboundOrder", StringComparison.OrdinalIgnoreCase))
        {
            await CompleteInboundOrderByNoAsync(order.RefType!, order.RefId, orderId, ct);
            return;
        }

        if (string.Equals(order.RefType, "OutboundOrder", StringComparison.OrdinalIgnoreCase))
        {
            await CompletePickingTasksAsync(order.RefId!, ct);
            await CompleteOutboundIfReadyAsync(order.RefType!, order.RefId, orderId, ct);
        }
    }

    private async Task CompletePickingTasksAsync(string orderNo, CancellationToken ct)
    {
        var outbound = await _db.WmsOutboundOrders
            .Include(x => x.PickingTasks)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.OrderNo == orderNo, ct);
        if (outbound == null) return;

        foreach (var task in outbound.PickingTasks.Where(t =>
                     t.Status is WmsPickingTaskStatus.Transporting or WmsPickingTaskStatus.Booked or WmsPickingTaskStatus.Confirmed))
        {
            if (task.PickQty <= 0) task.PickQty = task.BookQty;
            task.Status = WmsPickingTaskStatus.Completed;
            task.ModifyDate = DateTime.UtcNow;
            var line = outbound.Lines.FirstOrDefault(l => l.Id == task.LineId);
            if (line != null && line.CompletedQty < line.Qty)
                line.CompletedQty = Math.Min(line.Qty, line.CompletedQty + task.PickQty);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task CompleteInboundDetailAsync(string detailIdText, Guid completedOrderId, CancellationToken ct)
    {
        if (!int.TryParse(detailIdText, out var detailId))
        {
            _logger?.LogWarning("Invalid InboundDetail RefId {RefId}", detailIdText);
            return;
        }

        var detail = await _db.WmsInboundDetails.FirstOrDefaultAsync(x => x.Id == detailId, ct);
        if (detail == null) return;

        detail.Status = WmsInboundDetailStatus.Completed;
        detail.TransportOrderId ??= completedOrderId;
        detail.ModifyDate = DateTime.UtcNow;

        var inbound = await _db.WmsInboundOrders
            .Include(x => x.Lines)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == detail.OrderId, ct);
        if (inbound != null
            && inbound.Lines.All(x => x.CompletedQty >= x.Qty)
            && inbound.Details.All(d =>
                d.Id == detail.Id
                || d.Status is WmsInboundDetailStatus.Completed or WmsInboundDetailStatus.Cancelled))
        {
            inbound.Status = WmsOrderStatus.Completed;
            inbound.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task CompleteInboundOrderByNoAsync(string refType, string refId, Guid completedOrderId, CancellationToken ct)
    {
        if (await HasPendingTransportAsync(refType, refId, completedOrderId, ct))
            return;

        var inbound = await _db.WmsInboundOrders
            .Include(x => x.Lines)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrderNo == refId, ct);
        if (inbound == null) return;

        foreach (var detail in inbound.Details.Where(d =>
                     d.Status is WmsInboundDetailStatus.Created or WmsInboundDetailStatus.Transporting))
        {
            detail.Status = WmsInboundDetailStatus.Completed;
            detail.ModifyDate = DateTime.UtcNow;
        }

        inbound.Status = WmsOrderStatus.Completed;
        inbound.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task CompleteOutboundIfReadyAsync(string refType, string refId, Guid completedOrderId, CancellationToken ct)
    {
        if (await HasPendingTransportAsync(refType, refId, completedOrderId, ct))
            return;

        var outbound = await _db.WmsOutboundOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.OrderNo == refId, ct);
        if (outbound == null) return;

        foreach (var line in outbound.Lines.Where(l => l.CompletedQty < l.Qty))
            line.CompletedQty = line.Qty;

        outbound.Status = WmsOrderStatus.Completed;
        outbound.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private Task<bool> HasPendingTransportAsync(string refType, string refId, Guid completedOrderId, CancellationToken ct) =>
        _db.BusTransportOrders.AnyAsync(x =>
            x.Id != completedOrderId
            && x.RefType == refType
            && x.RefId == refId
            && x.Status != BusOrderStatus.Completed
            && x.Status != BusOrderStatus.Failed, ct);

    private async Task OccupyTargetLocationAsync(string toLoc, string? containerCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toLoc)) return;
        var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == toLoc, ct);
        if (loc == null) return;
        loc.IsBooked = false;
        loc.IsOccupied = true;
        loc.CurrentContainerCode = containerCode;
        loc.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task ReleaseSourceLocationAsync(string fromLoc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fromLoc)) return;
        var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == fromLoc, ct);
        if (loc == null) return;
        loc.IsOccupied = false;
        loc.IsBooked = false;
        loc.CurrentContainerCode = null;
        loc.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task MoveStockIfNeededAsync(
        string fromLoc,
        string toLoc,
        string? containerCode,
        string? refType,
        string? refId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fromLoc) || string.IsNullOrWhiteSpace(toLoc)
            || string.Equals(fromLoc, toLoc, StringComparison.OrdinalIgnoreCase))
            return;

        var alreadyAtTarget = await _db.WmsStocks.AnyAsync(s =>
            s.LocationCode == toLoc
            && s.ContainerCode == containerCode
            && s.Qty > 0, ct);
        if (alreadyAtTarget)
            return;

        var stocks = await _db.WmsStocks
            .Where(s => s.LocationCode == fromLoc && s.ContainerCode == containerCode && s.Qty > 0)
            .ToListAsync(ct);
        foreach (var stock in stocks)
        {
            var qty = stock.Qty;
            if (qty <= 0) continue;
            if (stock.AvailableQty < qty)
                stock.AvailableQty = qty;

            await _stock.ShipAsync(new ShipStockRequest(
                fromLoc,
                stock.MaterialCode,
                qty,
                stock.ContainerCode,
                Reason: string.Equals(refType, "OutboundOrder", StringComparison.OrdinalIgnoreCase) ? "OutboundShip" : "Putaway",
                RefType: refType,
                RefId: refId), ct);
            await _stock.ReceiveAsync(new ReceiveStockRequest(
                toLoc,
                stock.MaterialCode,
                qty,
                stock.ContainerCode,
                Reason: string.Equals(refType, "OutboundOrder", StringComparison.OrdinalIgnoreCase) ? "OutboundArrive" : "Putaway",
                RefType: refType,
                RefId: refId), ct);
        }
    }
}
