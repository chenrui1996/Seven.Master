using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
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
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.UtcNow;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("更新成功");
    }
}
