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

public sealed class PickingService : IPickingService
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;
    private readonly ITransportOrderRequest? _transport;
    private readonly IWmsExtensionHooks _hooks;

    public PickingService(
        SevenDbContext db,
        IStockService stock,
        ITransportOrderRequest? transport = null,
        IWmsExtensionHooks? hooks = null)
    {
        _db = db;
        _stock = stock;
        _transport = transport;
        _hooks = hooks ?? NoOpWmsExtensionHooks.Instance;
    }

    public Task<PageGridData<WmsPickingTask>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsPickingTasks.AsNoTracking(), options, ct);

    public async Task<IReadOnlyList<WmsPickingTask>> ListPendingAsync(CancellationToken ct = default)
    {
        var list = await _db.WmsPickingTasks.AsNoTracking()
            .Where(x => x.Status == WmsPickingTaskStatus.Booked)
            .OrderBy(x => x.WcsPri).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return await _hooks.FilterPendingPickingAsync(list, ct);
    }

    public async Task<IReadOnlyList<WmsPickingTask>> GenerateFromOutboundAsync(int outboundOrderId, CancellationToken ct = default)
    {
        var order = await _db.WmsOutboundOrders
            .Include(x => x.Lines)
            .Include(x => x.PickingTasks)
            .FirstOrDefaultAsync(x => x.Id == outboundOrderId, ct)
            ?? throw new WmsDomainException(ExceptionCodes.Wms.OrderNotFound, "出库单不存在");

        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException(ExceptionCodes.Wms.OrderStatusIllegal, "仅已审核或执行中的出库单可生成拣选");

        if (order.PickingTasks.Any(t => t.Status != WmsPickingTaskStatus.Cancelled))
            return order.PickingTasks.Where(t => t.Status != WmsPickingTaskStatus.Cancelled).ToList();

        var created = new List<WmsPickingTask>();
        foreach (var line in order.Lines.OrderBy(x => x.WcsPri).ThenBy(x => x.LineNo))
        {
            var remaining = line.Qty - line.CompletedQty;
            if (remaining <= 0) continue;
            if (string.IsNullOrWhiteSpace(line.FromLocation))
                throw new WmsDomainException(ExceptionCodes.Wms.LocationRequired, $"行 {line.LineNo} 缺少发运库位");

            await _stock.BookAsync(new BookStockRequest(
                line.FromLocation!,
                line.MaterialCode,
                remaining,
                line.ContainerCode,
                Reason: "PickingBook",
                RefType: "OutboundOrder",
                RefId: order.OrderNo), ct);

            var task = new WmsPickingTask
            {
                TaskNo = $"{order.OrderNo}-{line.LineNo:D3}",
                OutboundOrderId = order.Id,
                LineId = line.Id,
                MaterialCode = line.MaterialCode,
                BookQty = remaining,
                PickQty = 0,
                FromLocation = line.FromLocation,
                ToLocation = line.ToLocation,
                ContainerCode = line.ContainerCode,
                WcsPri = line.WcsPri > 0 ? line.WcsPri : line.LineNo,
                Status = WmsPickingTaskStatus.Booked,
                CreateDate = DateTime.UtcNow
            };
            _db.WmsPickingTasks.Add(task);
            created.Add(task);
        }

        if (created.Count > 0)
        {
            order.Status = WmsOrderStatus.Executing;
            order.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    public async Task<WmsPickingTask> ConfirmPickAsync(ConfirmPickRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var task = await _db.WmsPickingTasks
            .Include(x => x.Order)
            .Include(x => x.Line)
            .FirstOrDefaultAsync(x => x.Id == request.PickingTaskId, ct)
            ?? throw new WmsDomainException(ExceptionCodes.Wms.PickingNotFound, "拣选任务不存在");

        if (task.Status != WmsPickingTaskStatus.Booked)
            throw new WmsDomainException(ExceptionCodes.Wms.PickingStatusIllegal, "仅已预约的拣选任务可确认");

        var pickQty = request.PickQty ?? task.BookQty;
        if (pickQty <= 0 || pickQty > task.BookQty)
            throw new WmsDomainException(ExceptionCodes.Wms.QtyInvalid, "拣货数量无效");

        if (!string.IsNullOrWhiteSpace(request.ContainerCode))
            task.ContainerCode = request.ContainerCode.Trim();
        if (!string.IsNullOrWhiteSpace(request.FromLocation))
            task.FromLocation = request.FromLocation.Trim();
        if (string.IsNullOrWhiteSpace(task.FromLocation))
            throw new WmsDomainException(ExceptionCodes.Wms.LocationRequired, "缺少拣货库位");

        task.PickQty = pickQty;
        task.ModifyDate = DateTime.UtcNow;

        var needsTransport = request.DispatchTransport
            && _transport is { IsEnabled: true }
            && !string.IsNullOrWhiteSpace(task.ToLocation)
            && !string.Equals(task.FromLocation, task.ToLocation, StringComparison.OrdinalIgnoreCase);

        if (needsTransport)
        {
            var order = task.Order ?? await _db.WmsOutboundOrders.FirstAsync(x => x.Id == task.OutboundOrderId, ct);
            await _transport!.RequestAsync(new TransportOrderHookRequest(
                task.FromLocation!,
                task.ToLocation!,
                task.ContainerCode,
                "OutboundOrder",
                order.OrderNo,
                order.WcsGroupNo ?? order.OrderNo,
                task.WcsPri), ct);
            task.Status = WmsPickingTaskStatus.Transporting;
        }
        else
        {
            await _stock.ConfirmPickAsync(new ConfirmPickStockRequest(
                task.FromLocation!,
                task.MaterialCode,
                pickQty,
                task.ContainerCode,
                Reason: "ConfirmPick",
                RefType: "PickingTask",
                RefId: task.TaskNo), ct);

            if (task.Line != null)
                task.Line.CompletedQty += pickQty;
            task.Status = WmsPickingTaskStatus.Completed;
            await TryCompleteOutboundAsync(task.OutboundOrderId, ct);
        }

        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task CancelAsync(int pickingTaskId, CancellationToken ct = default)
    {
        var task = await _db.WmsPickingTasks.FirstOrDefaultAsync(x => x.Id == pickingTaskId, ct)
            ?? throw new WmsDomainException(ExceptionCodes.Wms.PickingNotFound, "拣选任务不存在");

        if (task.Status is not WmsPickingTaskStatus.Booked)
            throw new WmsDomainException(ExceptionCodes.Wms.PickingStatusIllegal, "仅已预约的拣选可取消");

        if (!string.IsNullOrWhiteSpace(task.FromLocation) && task.BookQty > 0)
        {
            await _stock.ReleaseBookAsync(new BookStockRequest(
                task.FromLocation!,
                task.MaterialCode,
                task.BookQty,
                task.ContainerCode,
                Reason: "CancelPick",
                RefType: "PickingTask",
                RefId: task.TaskNo), ct);
        }

        task.Status = WmsPickingTaskStatus.Cancelled;
        task.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task TryCompleteOutboundAsync(int outboundOrderId, CancellationToken ct)
    {
        var order = await _db.WmsOutboundOrders
            .Include(x => x.Lines)
            .Include(x => x.PickingTasks)
            .FirstOrDefaultAsync(x => x.Id == outboundOrderId, ct);
        if (order == null) return;

        var active = order.PickingTasks.Where(t => t.Status != WmsPickingTaskStatus.Cancelled).ToList();
        if (active.Count > 0 && active.All(t => t.Status == WmsPickingTaskStatus.Completed)
            && order.Lines.All(l => l.CompletedQty >= l.Qty))
        {
            order.Status = WmsOrderStatus.Completed;
            order.ModifyDate = DateTime.UtcNow;
        }
    }
}
