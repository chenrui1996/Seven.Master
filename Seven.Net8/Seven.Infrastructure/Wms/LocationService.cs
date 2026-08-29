using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class LocationService : ILocationService
{
    private readonly SevenDbContext _db;

    public LocationService(SevenDbContext db) => _db = db;

    public Task<WmsLocation?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.WmsLocations.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct);

    public Task<PageGridData<WmsLocation>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsLocations.AsNoTracking(), options, ct);

    public async Task<WebResponseContent> AddAsync(WmsLocation entity, CancellationToken ct = default)
    {
        try
        {
            await NormalizeAndValidateAsync(entity, ct);
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }

        entity.CreateDate = DateTime.UtcNow;
        entity.IsDeleted = false;
        _db.WmsLocations.Add(entity);
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(WmsLocation entity, CancellationToken ct = default)
    {
        var existing = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        try
        {
            await NormalizeAndValidateAsync(entity, ct);
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }

        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.UtcNow;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("更新成功");
    }

    private async Task NormalizeAndValidateAsync(WmsLocation entity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.PackId))
            entity.PackId = PackCodeRules.TryDetectPackId(entity.Code) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(entity.PackId))
            throw new WmsDomainException("库位必须指定 PackId，或使用带前缀的编码（如 Stk./Fw.）");

        var detected = PackCodeRules.TryDetectPackId(entity.Code);
        if (detected != null && !string.Equals(detected, entity.PackId, StringComparison.OrdinalIgnoreCase))
            throw new WmsDomainException($"库位编码前缀与 PackId 不匹配: {entity.Code} / {entity.PackId}");

        entity.Code = PackCodeRules.EnsurePrefix(entity.Code, entity.PackId);
        if (!PackCodeRules.HasValidPrefix(entity.Code, entity.PackId))
            throw new WmsDomainException($"库位编码前缀无效: {entity.Code}");

        var warehouse = await _db.WmsWarehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.WarehouseId, ct)
            ?? throw new WmsDomainException("仓库不存在");
        WarehousePackRules.EnsureAtLeastOne(warehouse.EnabledPackIds);
        if (!WarehousePackRules.Allows(warehouse.EnabledPackIds, entity.PackId))
            throw new WmsDomainException($"仓库未启用 Pack「{entity.PackId}」，EnabledPackIds={warehouse.EnabledPackIds}");
    }
}
