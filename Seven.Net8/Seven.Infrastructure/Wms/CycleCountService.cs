using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class CycleCountService : ICycleCountService
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;

    public CycleCountService(SevenDbContext db, IStockService stock)
    {
        _db = db;
        _stock = stock;
    }

    public Task<PageGridData<WmsCycleCount>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsCycleCounts.AsNoTracking(), options, ct);

    public async Task<WmsCycleCount> CreatePlanAsync(CreateCycleCountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OrderNo))
            throw new WmsDomainException("单号不能为空");
        if (request.Lines == null || request.Lines.Count == 0)
            throw new WmsDomainException("盘点单至少一行");
        if (await _db.WmsCycleCounts.AnyAsync(x => x.OrderNo == request.OrderNo, ct))
            throw new WmsDomainException($"单号已存在: {request.OrderNo}");

        var order = new WmsCycleCount
        {
            OrderNo = request.OrderNo.Trim(),
            Status = WmsOrderStatus.Draft,
            CreateDate = DateTime.UtcNow
        };
        foreach (var line in request.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.LocationCode) || string.IsNullOrWhiteSpace(line.MaterialCode))
                throw new WmsDomainException("盘点行库位与物料不能为空");

            var stock = await _db.WmsStocks.FirstOrDefaultAsync(s =>
                s.LocationCode == line.LocationCode
                && s.MaterialCode == line.MaterialCode
                && s.ContainerCode == line.ContainerCode, ct);

            order.Lines.Add(new WmsCycleCountLine
            {
                LineNo = line.LineNo,
                LocationCode = line.LocationCode,
                MaterialCode = line.MaterialCode,
                ContainerCode = line.ContainerCode,
                BookQty = stock?.Qty ?? 0
            });
        }

        _db.WmsCycleCounts.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task RecordCountAsync(int orderId, int lineNo, decimal countQty, CancellationToken ct = default)
    {
        if (countQty < 0)
            throw new WmsDomainException("实盘数量不能为负");

        var order = await LoadAsync(orderId, ct);
        if (order.Status is WmsOrderStatus.Completed or WmsOrderStatus.Cancelled)
            throw new WmsDomainException("已完成或已取消的盘点单不可录入");

        var line = order.Lines.FirstOrDefault(x => x.LineNo == lineNo)
            ?? throw new WmsDomainException($"盘点行不存在: {lineNo}");
        line.CountQty = countQty;
        line.DiffQty = countQty - line.BookQty;
        line.Counted = true;
        if (order.Status == WmsOrderStatus.Draft)
            order.Status = WmsOrderStatus.Executing;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ConfirmAdjustAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status is WmsOrderStatus.Completed or WmsOrderStatus.Cancelled)
            throw new WmsDomainException("盘点单已结束");
        if (order.Status != WmsOrderStatus.Executing)
            throw new WmsDomainException("请先录入实盘");
        if (order.Lines.Any(x => !x.Counted))
            throw new WmsDomainException("尚有未录入实盘的行");

        await WmsTransaction.ExecuteAsync(_db, async () =>
        {
            foreach (var line in order.Lines.OrderBy(x => x.LineNo))
            {
                if (line.DiffQty > 0)
                {
                    await _stock.ReceiveAsync(new ReceiveStockRequest(
                        line.LocationCode,
                        line.MaterialCode,
                        line.DiffQty,
                        line.ContainerCode,
                        Reason: "CycleCountAdjust",
                        RefType: "CycleCount",
                        RefId: order.OrderNo), ct);
                }
                else if (line.DiffQty < 0)
                {
                    await _stock.ShipAsync(new ShipStockRequest(
                        line.LocationCode,
                        line.MaterialCode,
                        -line.DiffQty,
                        line.ContainerCode,
                        Reason: "CycleCountAdjust",
                        RefType: "CycleCount",
                        RefId: order.OrderNo), ct);
                }
            }

            order.Status = WmsOrderStatus.Completed;
            order.ModifyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }, ct);
    }

    private async Task<WmsCycleCount> LoadAsync(int orderId, CancellationToken ct)
    {
        var order = await _db.WmsCycleCounts
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
            throw new WmsDomainException("盘点单不存在");
        return order;
    }
}
