using Microsoft.EntityFrameworkCore;
using Seven.Application.Scada;
using Seven.Domain.Common;
using Seven.Domain.Entities.Platform;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Scada;

public sealed class ScadaViewService : IScadaViewService
{
    private readonly SevenDbContext _db;

    public ScadaViewService(SevenDbContext db) => _db = db;

    public Task<ScdView?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.ScdViews.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct);

    public Task<ScdView?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.ScdViews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PageGridData<ScdView>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.ScdViews.AsNoTracking(), options, ct);

    public async Task<WebResponseContent> AddAsync(ScdView entity, CancellationToken ct = default)
    {
        entity.CreateDate = DateTime.UtcNow;
        entity.IsDeleted = false;
        _db.ScdViews.Add(entity);
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(ScdView entity, CancellationToken ct = default)
    {
        var existing = await _db.ScdViews.FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.UtcNow;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("更新成功");
    }

    public async Task<ScdViewStatusDto?> GetStatusAsync(int viewId, CancellationToken ct = default)
    {
        var view = await _db.ScdViews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == viewId, ct);
        if (view == null) return null;

        var binds = await _db.ScdNodeBinds.AsNoTracking()
            .Where(x => x.ViewId == viewId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        var locationCodes = binds.Select(x => x.LocationCode).Distinct().ToList();
        var occupiedMap = locationCodes.Count == 0
            ? new Dictionary<string, bool>()
            : await _db.WmsLocations.AsNoTracking()
                .Where(x => locationCodes.Contains(x.Code))
                .ToDictionaryAsync(x => x.Code, x => x.IsOccupied, ct);

        var nodes = binds.Select(b => new ScdNodeStatusDto(
            b.Id,
            b.LocationCode,
            b.X,
            b.Y,
            b.Label,
            occupiedMap.GetValueOrDefault(b.LocationCode))).ToList();

        return new ScdViewStatusDto(view.Id, view.Code, view.Name, view.Width, view.Height, nodes);
    }
}
