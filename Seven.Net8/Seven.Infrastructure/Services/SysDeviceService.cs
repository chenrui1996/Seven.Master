using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Business;
using Seven.Infrastructure.Excel;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Security;

namespace Seven.Infrastructure.Services;

/// <summary>Device 服务（代码生成）</summary>
public class SysDeviceService : ISysDeviceService
{
    private readonly SevenDbContext _db;
    private readonly IDataScopeService _dataScope;
    private readonly ICurrentUserService _currentUser;

    public SysDeviceService(SevenDbContext db, IDataScopeService dataScope, ICurrentUserService currentUser)
    {
        _db = db;
        _dataScope = dataScope;
        _currentUser = currentUser;
    }

    public async Task<PageGridData<Device>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<Device>().AsNoTracking();
        var allowed = await _dataScope.GetAllowedUserIdsAsync(cancellationToken);
        query = DataScopeService.FilterByCreateIds(query, allowed);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    public async Task<WebResponseContent> AddAsync(Device entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        entity.CreateId = _currentUser.UserId;
        entity.Creator = _currentUser.UserName;
        entity.IsDeleted = false;
        _db.Set<Device>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(Device entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Set<Device>().FirstOrDefaultAsync(x => x.DeviceId == entity.DeviceId && !x.IsDeleted, cancellationToken);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.Now;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("更新成功");
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var list = await _db.Set<Device>().Where(x => ids.Contains(x.DeviceId) && !x.IsDeleted).ToListAsync(cancellationToken);
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
        return CrudExcelHelper.Export(page.Rows, nameof(Device.DeviceId));
    }

    public byte[] ExportTemplate() =>
        CrudExcelHelper.BuildTemplate<Device>(nameof(Device.DeviceId));

    public async Task<WebResponseContent> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var rows = CrudExcelHelper.Import<Device>(stream, nameof(Device.DeviceId));
        if (rows.Count == 0)
            return WebResponseContent.Error("未读取到有效数据");

        var ok = 0;
        var fail = 0;
        foreach (var entity in rows)
        {
            try
            {
                entity.DeviceId = default;
                entity.CreateDate = DateTime.Now;
                entity.IsDeleted = false;
                _db.Set<Device>().Add(entity);
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
