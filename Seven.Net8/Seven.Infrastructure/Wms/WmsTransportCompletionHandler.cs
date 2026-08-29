using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seven.Application.Wms;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wms;

/// <summary>运输单完成后：移库（收货位→目标 / 货位→出库口），并回写入/出库单完成。</summary>
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

        if (string.IsNullOrWhiteSpace(order.RefId))
            return;

        if (string.Equals(order.RefType, "InboundOrder", StringComparison.OrdinalIgnoreCase))
        {
            await CompleteInboundIfReadyAsync(order.RefType!, order.RefId, orderId, ct);
            return;
        }

        if (string.Equals(order.RefType, "OutboundOrder", StringComparison.OrdinalIgnoreCase))
            await CompleteOutboundIfReadyAsync(order.RefType!, order.RefId, orderId, ct);
    }

    private async Task CompleteInboundIfReadyAsync(string refType, string refId, Guid completedOrderId, CancellationToken ct)
    {
        if (await HasPendingTransportAsync(refType, refId, completedOrderId, ct))
            return;

        var inbound = await _db.WmsInboundOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.OrderNo == refId, ct);
        if (inbound == null) return;

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
            // 预留后 AvailableQty 可能小于 Qty，出库移库仍按账面 Qty 扣减
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
