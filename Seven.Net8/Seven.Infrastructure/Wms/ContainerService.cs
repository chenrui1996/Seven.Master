using Microsoft.EntityFrameworkCore;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class ContainerService : IContainerService
{
    private readonly SevenDbContext _db;

    public ContainerService(SevenDbContext db) => _db = db;

    public Task<WmsContainer?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.WmsContainers.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct);

    public Task<PageGridData<WmsContainer>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsContainers.AsNoTracking(), options, ct);

    public async Task<WebResponseContent> AddAsync(WmsContainer entity, CancellationToken ct = default)
    {
        entity.CreateDate = DateTime.UtcNow;
        entity.IsDeleted = false;
        _db.WmsContainers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(WmsContainer entity, CancellationToken ct = default)
    {
        var existing = await _db.WmsContainers.FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.UtcNow;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("更新成功");
    }
}
