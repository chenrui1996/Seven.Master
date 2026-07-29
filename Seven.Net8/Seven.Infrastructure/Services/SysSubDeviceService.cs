using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Business;
using Seven.Infrastructure.Excel;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Services;

/// <summary>SubDevice 服务（Device 子表 Demo）</summary>
public class SysSubDeviceService : ISysSubDeviceService
{
    private readonly SevenDbContext _db;

    public SysSubDeviceService(SevenDbContext db) => _db = db;

    public async Task<PageGridData<SubDevice>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<SubDevice>().AsNoTracking().Where(x => !x.IsDeleted);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    public async Task<WebResponseContent> AddAsync(SubDevice entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        entity.IsDeleted = false;
        _db.Set<SubDevice>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(SubDevice entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Set<SubDevice>()
            .FirstOrDefaultAsync(x => x.SubDeviceId == entity.SubDeviceId && !x.IsDeleted, cancellationToken);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.Now;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("更新成功");
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var list = await _db.Set<SubDevice>().Where(x => ids.Contains(x.SubDeviceId) && !x.IsDeleted).ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    public async Task<byte[]> ExportAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        options.Page = 1;
        options.Rows = CrudExcelHelper.MaxExportRows;
        var page = await GetPageDataAsync(options, cancellationToken);
        return CrudExcelHelper.Export(page.Rows, nameof(SubDevice.SubDeviceId));
    }

    public byte[] ExportTemplate() =>
        CrudExcelHelper.BuildTemplate<SubDevice>(nameof(SubDevice.SubDeviceId));

    public async Task<WebResponseContent> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var rows = CrudExcelHelper.Import<SubDevice>(stream, nameof(SubDevice.SubDeviceId));
        if (rows.Count == 0)
            return WebResponseContent.Error("未读取到有效数据");

        var ok = 0;
        var fail = 0;
        foreach (var entity in rows)
        {
            try
            {
                entity.SubDeviceId = default;
                entity.CreateDate = DateTime.Now;
                entity.IsDeleted = false;
                _db.Set<SubDevice>().Add(entity);
                await _db.SaveChangesAsync(cancellationToken);
                ok++;
            }
            catch
            {
                fail++;
                _db.ChangeTracker.Clear();
            }
        }

        return WebResponseContent.Ok($"导入完成：成功 {ok} 条，失败 {fail} 条", new { ok, fail });
    }
}
