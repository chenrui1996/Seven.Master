using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class InboundOrderService : IInboundOrderService
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;
    private readonly ITransportOrderRequest? _transport;

    public InboundOrderService(
        SevenDbContext db,
        IStockService stock,
        ITransportOrderRequest? transport = null)
    {
        _db = db;
        _stock = stock;
        _transport = transport;
    }

    public Task<PageGridData<WmsInboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsInboundOrders.AsNoTracking(), options, ct);

    public async Task<WmsInboundOrder> CreateAsync(CreateInboundOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrderNo(request.OrderNo);
        if (request.Lines == null || request.Lines.Count == 0)
            throw new WmsDomainException("入库单至少一行");
        if (await _db.WmsInboundOrders.AnyAsync(x => x.OrderNo == request.OrderNo, ct))
            throw new WmsDomainException($"单号已存在: {request.OrderNo}");

        var order = new WmsInboundOrder
        {
            OrderNo = request.OrderNo.Trim(),
            OrderType = request.OrderType,
            Status = WmsOrderStatus.Draft,
            CreateDate = DateTime.UtcNow
        };
        foreach (var line in request.Lines)
        {
            ValidateQty(line.Qty);
            if (string.IsNullOrWhiteSpace(line.MaterialCode))
                throw new WmsDomainException("物料编码不能为空");
            order.Lines.Add(new WmsInboundOrderLine
            {
                LineNo = line.LineNo,
                MaterialCode = line.MaterialCode,
                Qty = line.Qty,
                ContainerCode = line.ContainerCode,
                FromLocation = line.FromLocation,
                ToLocation = line.ToLocation
            });
        }

        _db.WmsInboundOrders.Add(order);
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
    }

    public async Task ReceiveAndBuildPalletAsync(
        int orderId,
        ReceiveAndBuildPalletRequest? request = null,
        CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException("仅已审核或执行中的入库单可收货组盘");

        var requestedTransport = false;
        await WmsTransaction.ExecuteAsync(_db, async () =>
        {
            order.Status = WmsOrderStatus.Executing;
            foreach (var line in order.Lines.OrderBy(x => x.LineNo))
            {
                var remaining = line.Qty - line.CompletedQty;
                if (remaining <= 0) continue;

                var receiveLoc = ResolveReceiveLocation(request, line);
                var targetLoc = string.IsNullOrWhiteSpace(line.ToLocation) ? receiveLoc : line.ToLocation;
                if (string.IsNullOrWhiteSpace(receiveLoc))
                    throw new WmsDomainException($"行 {line.LineNo} 缺少收货库位");

                await EnsureContainerAsync(line, receiveLoc, ct);
                await _stock.ReceiveAsync(new ReceiveStockRequest(
                    receiveLoc,
                    line.MaterialCode,
                    remaining,
                    line.ContainerCode,
                    Reason: "InboundReceive",
                    RefType: "InboundOrder",
                    RefId: order.OrderNo), ct);
                line.CompletedQty += remaining;
                requestedTransport |= WillRequestTransport(receiveLoc, targetLoc, line.ContainerCode);
            }

            if (!requestedTransport && order.Lines.All(x => x.CompletedQty >= x.Qty))
                order.Status = WmsOrderStatus.Completed;

            await _db.SaveChangesAsync(ct);
        }, ct);

        if (!requestedTransport || _transport == null) return;
        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            var receiveLoc = ResolveReceiveLocation(request, line);
            var targetLoc = string.IsNullOrWhiteSpace(line.ToLocation) ? receiveLoc : line.ToLocation;
            if (!WillRequestTransport(receiveLoc, targetLoc, line.ContainerCode))
                continue;
            await _transport.RequestAsync(new TransportOrderHookRequest(
                receiveLoc,
                targetLoc,
                line.ContainerCode,
                "InboundOrder",
                order.OrderNo), ct);
        }
    }

    private bool WillRequestTransport(string? receiveLoc, string? targetLoc, string? containerCode) =>
        _transport is { IsEnabled: true }
        && !string.IsNullOrWhiteSpace(containerCode)
        && !string.IsNullOrWhiteSpace(receiveLoc)
        && !string.IsNullOrWhiteSpace(targetLoc)
        && !string.Equals(receiveLoc, targetLoc, StringComparison.OrdinalIgnoreCase);

    private static string ResolveReceiveLocation(ReceiveAndBuildPalletRequest? request, WmsInboundOrderLine line)
    {
        if (!string.IsNullOrWhiteSpace(request?.ReceiveLocationCode))
            return request.ReceiveLocationCode;
        if (!string.IsNullOrWhiteSpace(line.FromLocation))
            return line.FromLocation;
        return line.ToLocation ?? string.Empty;
    }

    private async Task<WmsInboundOrder> LoadAsync(int orderId, CancellationToken ct)
    {
        var order = await _db.WmsInboundOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
            throw new WmsDomainException("入库单不存在");
        return order;
    }

    private async Task EnsureContainerAsync(WmsInboundOrderLine line, string receiveLoc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line.ContainerCode)) return;
        var exists = await _db.WmsContainers.AnyAsync(c => c.Code == line.ContainerCode, ct);
        if (exists) return;
        _db.WmsContainers.Add(new WmsContainer
        {
            Code = line.ContainerCode,
            LocationCode = receiveLoc,
            Status = WmsContainerStatus.Occupied,
            CreateDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateOrderNo(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            throw new WmsDomainException("单号不能为空");
    }

    private static void ValidateQty(decimal qty)
    {
        if (qty <= 0)
            throw new WmsDomainException("数量必须大于 0");
    }
}
