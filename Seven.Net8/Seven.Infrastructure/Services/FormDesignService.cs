using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Form;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Services;

public interface IFormDesignService
{
    Task<PageGridData<FormDesignOptions>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WebResponseContent> GetAsync(int formId, CancellationToken ct = default);
    Task<WebResponseContent> SaveAsync(FormDesignOptions entity, CancellationToken ct = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken ct = default);
    Task<WebResponseContent> SubmitCollectionAsync(int formId, string formData, CancellationToken ct = default);
    Task<PageGridData<FormCollectionObject>> GetCollectionsAsync(PageDataOptions options, CancellationToken ct = default);
}

public sealed class FormDesignService : IFormDesignService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FormDesignService(SevenDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PageGridData<FormDesignOptions>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(
            _db.FormDesignOptions.AsNoTracking().Where(x => !x.IsDeleted).OrderByDescending(x => x.FormId),
            options,
            ct);

    public async Task<WebResponseContent> GetAsync(int formId, CancellationToken ct = default)
    {
        var row = await _db.FormDesignOptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FormId == formId && !x.IsDeleted, ct);
        return row == null ? WebResponseContent.Error("表单不存在") : WebResponseContent.Ok(data: row);
    }

    public async Task<WebResponseContent> SaveAsync(FormDesignOptions entity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Title))
            return WebResponseContent.Error("表单名称不能为空");

        if (entity.FormId > 0)
        {
            var existing = await _db.FormDesignOptions.FirstOrDefaultAsync(x => x.FormId == entity.FormId && !x.IsDeleted, ct);
            if (existing == null) return WebResponseContent.Error("表单不存在");
            existing.Title = entity.Title.Trim();
            existing.FormOptions = entity.FormOptions;
            existing.FormHtml = entity.FormHtml;
            existing.Enable = entity.Enable ?? 1;
            existing.ModifyDate = DateTime.Now;
            existing.Modifier = _currentUser.UserName;
        }
        else
        {
            entity.Title = entity.Title.Trim();
            entity.Enable ??= 1;
            entity.CreateDate = DateTime.Now;
            entity.Creator = _currentUser.UserName;
            entity.CreateId = _currentUser.UserId;
            entity.IsDeleted = false;
            _db.FormDesignOptions.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("保存成功", new { entity.FormId });
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken ct = default)
    {
        var rows = await _db.FormDesignOptions.Where(x => ids.Contains(x.FormId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var r in rows)
        {
            r.IsDeleted = true;
            r.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("删除成功");
    }

    public async Task<WebResponseContent> SubmitCollectionAsync(int formId, string formData, CancellationToken ct = default)
    {
        var form = await _db.FormDesignOptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FormId == formId && !x.IsDeleted && x.Enable == 1, ct);
        if (form == null) return WebResponseContent.Error("表单不存在或未启用");
        if (string.IsNullOrWhiteSpace(formData)) return WebResponseContent.Error("表单数据不能为空");

        _db.FormCollectionObjects.Add(new FormCollectionObject
        {
            FormId = formId,
            FormData = formData,
            Submitter = _currentUser.UserName,
            CreateDate = DateTime.Now,
            Creator = _currentUser.UserName,
            CreateId = _currentUser.UserId,
        });
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("提交成功");
    }

    public async Task<PageGridData<FormCollectionObject>> GetCollectionsAsync(PageDataOptions options, CancellationToken ct = default)
    {
        var query = _db.FormCollectionObjects.AsNoTracking().Where(x => !x.IsDeleted);
        var formIdRaw = ReadWhere(options.Wheres, "formId") ?? ReadWhere(options.Wheres, "FormId");
        if (int.TryParse(formIdRaw, out var formId) && formId > 0)
            query = query.Where(x => x.FormId == formId);
        query = query.OrderByDescending(x => x.FormCollectionId);
        return await CrudHelper.PaginateAsync(query, options, ct);
    }

    static string? ReadWhere(string? wheresJson, string name)
    {
        if (string.IsNullOrWhiteSpace(wheresJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(wheresJson);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return null;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var n)) continue;
                if (!string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase)) continue;
                return item.TryGetProperty("value", out var v) ? v.ToString() : null;
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
