using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class StockService : IStockService
{
    private readonly SevenDbContext _db;

    public StockService(SevenDbContext db) => _db = db;

    public async Task<WmsStock> ReceiveAsync(ReceiveStockRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateQty(request.Qty);
        var location = await RequireLocationAsync(request.LocationCode, ct);
        var stock = await FindOrCreateStockAsync(
            request.LocationCode, request.MaterialCode, request.ContainerCode, request.Lot, ct);

        stock.Qty += request.Qty;
        stock.AvailableQty += request.Qty;
        location.IsOccupied = true;
        if (!string.IsNullOrWhiteSpace(request.ContainerCode))
        {
            location.CurrentContainerCode = request.ContainerCode;
            await BindContainerAsync(request.ContainerCode, location.Code, ct);
        }

        AddLedger(stock, request.Qty, request.Reason ?? "Receive", request.RefType, request.RefId);
        await _db.SaveChangesAsync(ct);
        return stock;
    }

    public async Task<WmsStock> ShipAsync(ShipStockRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateQty(request.Qty);
        var stock = await FindStockAsync(
            request.LocationCode, request.MaterialCode, request.ContainerCode, request.Lot, ct);
        if (stock == null || stock.AvailableQty < request.Qty)
            throw new WmsDomainException("库存不足");

        stock.Qty -= request.Qty;
        stock.AvailableQty -= request.Qty;
        AddLedger(stock, -request.Qty, request.Reason ?? "Ship", request.RefType, request.RefId);

        if (stock.Qty <= 0)
            await ReleaseLocationIfEmptyAsync(request.LocationCode, stock.Id, ct);

        await _db.SaveChangesAsync(ct);
        return stock;
    }

    public Task<PageGridData<WmsStock>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsStocks.AsNoTracking(), options, ct);

    private static void ValidateQty(decimal qty)
    {
        if (qty <= 0)
            throw new WmsDomainException("数量必须大于 0");
    }

    private async Task<WmsLocation> RequireLocationAsync(string locationCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(locationCode))
            throw new WmsDomainException("库位编码不能为空");
        var location = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == locationCode, ct);
        if (location == null)
            throw new WmsDomainException($"库位不存在: {locationCode}");
        if (location.IsLocked)
            throw new WmsDomainException($"库位已锁定: {locationCode}");
        return location;
    }

    private Task<WmsStock?> FindStockAsync(
        string locationCode, string materialCode, string? containerCode, string? lot, CancellationToken ct) =>
        _db.WmsStocks.FirstOrDefaultAsync(s =>
            s.LocationCode == locationCode
            && s.MaterialCode == materialCode
            && s.ContainerCode == containerCode
            && s.Lot == lot, ct);

    private async Task<WmsStock> FindOrCreateStockAsync(
        string locationCode, string materialCode, string? containerCode, string? lot, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(materialCode))
            throw new WmsDomainException("物料编码不能为空");

        var existing = await FindStockAsync(locationCode, materialCode, containerCode, lot, ct);
        if (existing != null) return existing;

        var created = new WmsStock
        {
            LocationCode = locationCode,
            MaterialCode = materialCode,
            ContainerCode = containerCode,
            Lot = lot,
            Qty = 0,
            AvailableQty = 0
        };
        _db.WmsStocks.Add(created);
        return created;
    }

    private async Task BindContainerAsync(string containerCode, string locationCode, CancellationToken ct)
    {
        var container = await _db.WmsContainers.FirstOrDefaultAsync(x => x.Code == containerCode, ct);
        if (container == null) return;
        container.LocationCode = locationCode;
        if (container.Status == WmsContainerStatus.Empty)
            container.Status = WmsContainerStatus.Occupied;
    }

    private async Task ReleaseLocationIfEmptyAsync(string locationCode, int currentStockId, CancellationToken ct)
    {
        var remaining = await _db.WmsStocks.AnyAsync(
            s => s.LocationCode == locationCode && s.Id != currentStockId && s.Qty > 0, ct);
        if (remaining) return;
        var location = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == locationCode, ct);
        if (location == null) return;
        location.IsOccupied = false;
        location.CurrentContainerCode = null;
    }

    private void AddLedger(WmsStock stock, decimal deltaQty, string reason, string? refType, string? refId)
    {
        _db.WmsStockLedgers.Add(new WmsStockLedger
        {
            Stock = stock,
            MaterialCode = stock.MaterialCode,
            LocationCode = stock.LocationCode,
            ContainerCode = stock.ContainerCode,
            DeltaQty = deltaQty,
            Reason = reason,
            RefType = refType,
            RefId = refId,
            CreateDate = DateTime.UtcNow
        });
    }
}
