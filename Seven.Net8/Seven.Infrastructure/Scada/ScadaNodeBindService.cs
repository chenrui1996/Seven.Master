using Microsoft.EntityFrameworkCore;
using Seven.Application.Scada;
using Seven.Domain.Common;
using Seven.Domain.Entities.Platform;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Scada;

public sealed class ScadaNodeBindService : IScadaNodeBindService
{
    private readonly SevenDbContext _db;

    public ScadaNodeBindService(SevenDbContext db) => _db = db;

    public Task<PageGridData<ScdNodeBind>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.ScdNodeBinds.AsNoTracking(), options, ct);

    public async Task<IReadOnlyList<ScdNodeBind>> ListByViewIdAsync(int viewId, CancellationToken ct = default) =>
        await _db.ScdNodeBinds.AsNoTracking()
            .Where(x => x.ViewId == viewId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<WebResponseContent> AddAsync(ScdNodeBind entity, CancellationToken ct = default)
    {
        entity.CreateDate = DateTime.UtcNow;
        entity.IsDeleted = false;
        _db.ScdNodeBinds.Add(entity);
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(ScdNodeBind entity, CancellationToken ct = default)
    {
        var existing = await _db.ScdNodeBinds.FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.UtcNow;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("更新成功");
    }
}
