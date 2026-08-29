using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class OutboundOrderService : IOutboundOrderService
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;
    private readonly ITransportOrderRequest? _transport;

    public OutboundOrderService(
        SevenDbContext db,
        IStockService stock,
        ITransportOrderRequest? transport = null)
    {
        _db = db;
        _stock = stock;
        _transport = transport;
    }

    public Task<PageGridData<WmsOutboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsOutboundOrders.AsNoTracking(), options, ct);

    public async Task<WmsOutboundOrder> CreateAsync(CreateOutboundOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OrderNo))
            throw new WmsDomainException("单号不能为空");
        if (request.Lines == null || request.Lines.Count == 0)
            throw new WmsDomainException("出库单至少一行");
        if (await _db.WmsOutboundOrders.AnyAsync(x => x.OrderNo == request.OrderNo, ct))
            throw new WmsDomainException($"单号已存在: {request.OrderNo}");

        var order = new WmsOutboundOrder
        {
            OrderNo = request.OrderNo.Trim(),
            OrderType = request.OrderType,
            Status = WmsOrderStatus.Draft,
            CreateDate = DateTime.UtcNow
        };
        foreach (var line in request.Lines)
        {
            if (line.Qty <= 0)
                throw new WmsDomainException("数量必须大于 0");
            if (string.IsNullOrWhiteSpace(line.MaterialCode))
                throw new WmsDomainException("物料编码不能为空");
            order.Lines.Add(new WmsOutboundOrderLine
            {
                LineNo = line.LineNo,
                MaterialCode = line.MaterialCode,
                Qty = line.Qty,
                FromLocation = line.FromLocation,
                ToLocation = line.ToLocation,
                ContainerCode = line.ContainerCode
            });
        }

        _db.WmsOutboundOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task ApproveAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status != WmsOrderStatus.Draft)
            throw new WmsDomainException("仅草稿可审核");
        order.Status = WmsOrderStatus.Approved;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (_transport is not { IsEnabled: true })
            return;

        var needsTransport = false;
        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            if (!NeedsTransport(line)) continue;
            needsTransport = true;
            var remaining = line.Qty - line.CompletedQty;
            if (remaining <= 0) continue;
            if (string.IsNullOrWhiteSpace(line.FromLocation))
                throw new WmsDomainException($"行 {line.LineNo} 缺少发运库位");

            var stock = await FindStockAsync(line, ct);
            if (stock == null || stock.AvailableQty < remaining)
                throw new WmsDomainException("库存不足");
            stock.AvailableQty -= remaining;

            await _transport.RequestAsync(new TransportOrderHookRequest(
                line.FromLocation!,
                line.ToLocation!,
                line.ContainerCode,
                "OutboundOrder",
                order.OrderNo), ct);
        }

        if (!needsTransport) return;

        order.Status = WmsOrderStatus.Executing;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AllocateAndReserveAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status != WmsOrderStatus.Approved)
            throw new WmsDomainException("仅已审核的出库单可预留");

        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            var remaining = line.Qty - line.CompletedQty;
            if (remaining <= 0) continue;
            if (string.IsNullOrWhiteSpace(line.FromLocation))
                throw new WmsDomainException($"行 {line.LineNo} 缺少发运库位");

            var stock = await FindStockAsync(line, ct);
            if (stock == null || stock.AvailableQty < remaining)
                throw new WmsDomainException("库存不足");
            stock.AvailableQty -= remaining;
        }

        order.Status = WmsOrderStatus.Executing;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ShipAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException("仅已审核或执行中的出库单可发运");

        if (_transport is { IsEnabled: true } && order.Lines.Any(NeedsTransport))
            throw new WmsDomainException("已挂运输单，请等待运输完成后自动扣账，勿手动发运");

        await WmsTransaction.ExecuteAsync(_db, async () =>
        {
            order.Status = WmsOrderStatus.Executing;
            foreach (var line in order.Lines.OrderBy(x => x.LineNo))
            {
                var remaining = line.Qty - line.CompletedQty;
                if (remaining <= 0) continue;
                if (string.IsNullOrWhiteSpace(line.FromLocation))
                    throw new WmsDomainException($"行 {line.LineNo} 缺少发运库位");

                await ReleaseReservationIfNeededAsync(line, remaining, ct);
                await _stock.ShipAsync(new ShipStockRequest(
                    line.FromLocation,
                    line.MaterialCode,
                    remaining,
                    line.ContainerCode,
                    Reason: "OutboundShip",
                    RefType: "OutboundOrder",
                    RefId: order.OrderNo), ct);
                line.CompletedQty += remaining;
            }

            if (order.Lines.All(x => x.CompletedQty >= x.Qty))
                order.Status = WmsOrderStatus.Completed;

            await _db.SaveChangesAsync(ct);
        }, ct);
    }

    private static bool NeedsTransport(WmsOutboundOrderLine line) =>
        !string.IsNullOrWhiteSpace(line.FromLocation)
        && !string.IsNullOrWhiteSpace(line.ToLocation)
        && !string.Equals(line.FromLocation, line.ToLocation, StringComparison.OrdinalIgnoreCase);

    private async Task ReleaseReservationIfNeededAsync(WmsOutboundOrderLine line, decimal qty, CancellationToken ct)
    {
        var stock = await FindStockAsync(line, ct);
        if (stock == null) return;
        if (stock.AvailableQty < qty && stock.Qty >= qty)
            stock.AvailableQty += qty;
    }

    private Task<WmsStock?> FindStockAsync(WmsOutboundOrderLine line, CancellationToken ct) =>
        _db.WmsStocks.FirstOrDefaultAsync(s =>
            s.LocationCode == line.FromLocation
            && s.MaterialCode == line.MaterialCode
            && s.ContainerCode == line.ContainerCode, ct);

    private async Task<WmsOutboundOrder> LoadAsync(int orderId, CancellationToken ct)
    {
        var order = await _db.WmsOutboundOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
            throw new WmsDomainException("出库单不存在");
        return order;
    }
}
