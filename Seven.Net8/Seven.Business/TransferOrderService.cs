using Microsoft.EntityFrameworkCore;
using Seven.Application.Business;
using Seven.Application.Wms;
using Seven.Domain.Business;
using Seven.Domain.Common;
using Seven.Domain.Entities.Business;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Business;

/// <summary>
/// 仓内调拨样板：扩展逻辑集中在 Seven.Business，组合标准 IStockService。
/// 状态：Draft → Approved(Book) → Completed(ConfirmPick + Receive)。
/// </summary>
public sealed class TransferOrderService : ITransferOrderService
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;

    public TransferOrderService(SevenDbContext db, IStockService stock)
    {
        _db = db;
        _stock = stock;
    }

    public Task<PageGridData<TransferOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.TransferOrders.AsNoTracking(), options, ct);

    public Task<TransferOrder?> GetAsync(int orderId, CancellationToken ct = default) =>
        _db.TransferOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public async Task<TransferOrder> CreateAsync(CreateTransferOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OrderNo))
            throw new BizDomainException(ExceptionCodes.Biz.OrderNoRequired, "单号不能为空");
        if (await _db.TransferOrders.AnyAsync(x => x.OrderNo == request.OrderNo, ct))
            throw new BizDomainException(ExceptionCodes.Biz.OrderNoExists, $"单号已存在: {request.OrderNo}");

        var order = new TransferOrder
        {
            OrderNo = request.OrderNo.Trim(),
            Status = WmsOrderStatus.Draft,
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
            CreateDate = DateTime.UtcNow
        };

        if (request.Lines != null)
        {
            foreach (var line in request.Lines)
                order.Lines.Add(BuildLine(line));
        }

        _db.TransferOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task ApproveAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status != WmsOrderStatus.Draft)
            throw new BizDomainException(ExceptionCodes.Biz.OrderStatusIllegal, "仅草稿可审核");
        if (order.Lines.Count == 0)
            throw new BizDomainException(ExceptionCodes.Biz.LinesRequired, "调拨单至少一行（可在明细表补充后再审）");

        foreach (var line in order.Lines)
            ValidateLineLocations(line);

        // 先落状态，再预约；失败时由调用方事务/重试处理（教学样板保持简单）
        order.Status = WmsOrderStatus.Approved;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        foreach (var line in order.Lines)
        {
            await _stock.BookAsync(new BookStockRequest(
                line.FromLocation!,
                line.MaterialCode,
                line.Qty,
                line.ContainerCode,
                Reason: "TransferBook",
                RefType: "Biz_TransferOrder",
                RefId: order.OrderNo), ct);
        }
    }

    public async Task CompleteAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status != WmsOrderStatus.Approved)
            throw new BizDomainException(ExceptionCodes.Biz.OrderStatusIllegal, "仅已审核可完成调拨");
        if (order.Lines.Count == 0)
            throw new BizDomainException(ExceptionCodes.Biz.LinesRequired, "调拨单至少一行");

        foreach (var line in order.Lines)
        {
            ValidateLineLocations(line);
            // 已 Book：用 ConfirmPick 扣 Qty，勿再用 Ship（Ship 要求 AvailableQty）
            await _stock.ConfirmPickAsync(new ConfirmPickStockRequest(
                line.FromLocation!,
                line.MaterialCode,
                line.Qty,
                line.ContainerCode,
                Reason: "TransferConfirm",
                RefType: "Biz_TransferOrder",
                RefId: order.OrderNo), ct);

            await _stock.ReceiveAsync(new ReceiveStockRequest(
                line.ToLocation!,
                line.MaterialCode,
                line.Qty,
                line.ContainerCode,
                Reason: "TransferReceive",
                RefType: "Biz_TransferOrder",
                RefId: order.OrderNo), ct);

            line.CompletedQty = line.Qty;
        }

        order.Status = WmsOrderStatus.Completed;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<TransferOrder> LoadAsync(int orderId, CancellationToken ct)
    {
        var order = await _db.TransferOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
            throw new BizDomainException(ExceptionCodes.Biz.OrderNotFound, $"调拨单不存在: {orderId}");
        return order;
    }

    private static TransferOrderLine BuildLine(TransferLineInput line)
    {
        if (line.Qty <= 0)
            throw new BizDomainException(ExceptionCodes.Biz.QtyInvalid, "数量必须大于 0");
        if (string.IsNullOrWhiteSpace(line.MaterialCode))
            throw new BizDomainException(ExceptionCodes.Biz.MaterialRequired, "物料编码不能为空");

        return new TransferOrderLine
        {
            LineNo = line.LineNo,
            MaterialCode = line.MaterialCode.Trim(),
            Qty = line.Qty,
            FromLocation = NullIfEmpty(line.FromLocation),
            ToLocation = NullIfEmpty(line.ToLocation),
            ContainerCode = NullIfEmpty(line.ContainerCode)
        };
    }

    private static void ValidateLineLocations(TransferOrderLine line)
    {
        if (string.IsNullOrWhiteSpace(line.FromLocation) || string.IsNullOrWhiteSpace(line.ToLocation))
            throw new BizDomainException(ExceptionCodes.Biz.LocationRequired, $"行 {line.LineNo} 需要 From/To 库位");
        if (string.Equals(line.FromLocation, line.ToLocation, StringComparison.OrdinalIgnoreCase))
            throw new BizDomainException(ExceptionCodes.Biz.LocationRequired, $"行 {line.LineNo} 源/目标库位不能相同");
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
