using Microsoft.EntityFrameworkCore;
using Seven.Application.Business;
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
    private readonly IPickingService _picking;
    private readonly ITransportOrderRequest? _transport;
    private readonly IWmsExtensionHooks _hooks;

    public OutboundOrderService(
        SevenDbContext db,
        IStockService stock,
        IPickingService picking,
        ITransportOrderRequest? transport = null,
        IWmsExtensionHooks? hooks = null)
    {
        _db = db;
        _stock = stock;
        _picking = picking;
        _transport = transport;
        _hooks = hooks ?? NoOpWmsExtensionHooks.Instance;
    }

    public Task<PageGridData<WmsOutboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsOutboundOrders.AsNoTracking(), options, ct);

    public Task<WmsOutboundOrder?> GetAsync(int orderId, CancellationToken ct = default) =>
        _db.WmsOutboundOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.PickingTasks)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public async Task<WmsOutboundOrder> CreateAsync(CreateOutboundOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OrderNo))
            throw new WmsDomainException(ExceptionCodes.Wms.OrderNoRequired, "单号不能为空");
        if (request.Lines == null || request.Lines.Count == 0)
            throw new WmsDomainException(ExceptionCodes.Wms.LinesRequired, "出库单至少一行");
        if (await _db.WmsOutboundOrders.AnyAsync(x => x.OrderNo == request.OrderNo, ct))
            throw new WmsDomainException(ExceptionCodes.Wms.OrderNoExists, $"单号已存在: {request.OrderNo}");

        var order = new WmsOutboundOrder
        {
            OrderNo = request.OrderNo.Trim(),
            OrderType = request.OrderType,
            Status = WmsOrderStatus.Draft,
            WcsGroupNo = string.IsNullOrWhiteSpace(request.WcsGroupNo) ? null : request.WcsGroupNo.Trim(),
            CreateDate = DateTime.UtcNow
        };
        foreach (var line in request.Lines)
        {
            if (line.Qty <= 0)
                throw new WmsDomainException(ExceptionCodes.Wms.QtyInvalid, "数量必须大于 0");
            if (string.IsNullOrWhiteSpace(line.MaterialCode))
                throw new WmsDomainException(ExceptionCodes.Wms.MaterialRequired, "物料编码不能为空");
            order.Lines.Add(new WmsOutboundOrderLine
            {
                LineNo = line.LineNo,
                MaterialCode = line.MaterialCode,
                Qty = line.Qty,
                FromLocation = line.FromLocation,
                ToLocation = line.ToLocation,
                ContainerCode = line.ContainerCode,
                WcsPri = line.WcsPri > 0 ? line.WcsPri : line.LineNo
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
            throw new WmsDomainException(ExceptionCodes.Wms.OrderStatusIllegal, "仅草稿可审核");

        await _hooks.BeforeOutboundApproveAsync(order, ct);

        order.Status = WmsOrderStatus.Approved;
        order.ModifyDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(order.WcsGroupNo))
            order.WcsGroupNo = order.OrderNo;
        foreach (var line in order.Lines.Where(x => x.WcsPri <= 0))
            line.WcsPri = line.LineNo;
        await _db.SaveChangesAsync(ct);

        // 审核即生成拣选并预约库存；建运改到拣选确认后
        await _picking.GenerateFromOutboundAsync(orderId, ct);

        await _hooks.AfterOutboundApproveAsync(order, ct);
    }

    public async Task AllocateAndReserveAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException(ExceptionCodes.Wms.OrderStatusIllegal, "仅已审核或执行中的出库单可预留");

        await _picking.GenerateFromOutboundAsync(orderId, ct);
    }

    public async Task ShipAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException(ExceptionCodes.Wms.OrderStatusIllegal, "仅已审核或执行中的出库单可发运");

        var picks = await _db.WmsPickingTasks
            .Where(x => x.OutboundOrderId == orderId && x.Status != WmsPickingTaskStatus.Cancelled)
            .ToListAsync(ct);

        if (picks.Any(p => p.Status == WmsPickingTaskStatus.Transporting)
            || (_transport is { IsEnabled: true } && order.Lines.Any(NeedsTransport) && picks.Any(p => p.Status == WmsPickingTaskStatus.Booked)))
        {
            if (picks.Any(NeedsTransportPick))
                throw new WmsDomainException(ExceptionCodes.Wms.TransportPending, "已挂运输或待拣选下发，请确认拣选后等待运输完成，勿手动发运");
        }

        await WmsTransaction.ExecuteAsync(_db, async () =>
        {
            order.Status = WmsOrderStatus.Executing;
            foreach (var line in order.Lines.OrderBy(x => x.LineNo))
            {
                var remaining = line.Qty - line.CompletedQty;
                if (remaining <= 0) continue;
                if (string.IsNullOrWhiteSpace(line.FromLocation))
                    throw new WmsDomainException(ExceptionCodes.Wms.LocationRequired, $"行 {line.LineNo} 缺少发运库位");

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

            foreach (var pick in picks.Where(p => p.Status == WmsPickingTaskStatus.Booked))
            {
                pick.Status = WmsPickingTaskStatus.Completed;
                pick.PickQty = pick.BookQty;
                pick.ModifyDate = DateTime.UtcNow;
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

    private static bool NeedsTransportPick(WmsPickingTask task) =>
        !string.IsNullOrWhiteSpace(task.FromLocation)
        && !string.IsNullOrWhiteSpace(task.ToLocation)
        && !string.Equals(task.FromLocation, task.ToLocation, StringComparison.OrdinalIgnoreCase);

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
            .Include(x => x.PickingTasks)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
            throw new WmsDomainException(ExceptionCodes.Wms.OrderNotFound, "出库单不存在");
        return order;
    }
}
