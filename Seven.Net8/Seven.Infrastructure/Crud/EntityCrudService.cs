using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Seven.Domain.Common;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Crud;

/// <summary>通用实体 CRUD（软删；主键名约定为 Id，类型 int 或 Guid）。</summary>
public sealed class EntityCrudService<TEntity> where TEntity : class
{
    private readonly SevenDbContext _db;

    public EntityCrudService(SevenDbContext db) => _db = db;

    public Task<PageGridData<TEntity>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.Set<TEntity>().AsNoTracking(), options, ct);

    public async Task<WebResponseContent> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        EnsureNewGuidKey(entity);
        if (entity is BaseEntity be)
        {
            be.CreateDate ??= DateTime.UtcNow;
            be.IsDeleted = false;
        }

        _db.Set<TEntity>().Add(entity);
        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        var key = GetKeyValue(entity);
        if (key == null) return WebResponseContent.Error("主键无效");

        var existing = await FindByKeyAsync(key, ct);
        if (existing == null) return WebResponseContent.Error("数据不存在");

        _db.Entry(existing).CurrentValues.SetValues(entity);
        if (existing is BaseEntity be)
        {
            be.ModifyDate = DateTime.UtcNow;
            be.IsDeleted = false;
        }

        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok("更新成功");
    }

    /// <summary>删除：请求体为 int[] / Guid[] / string[]（Guid 字符串）。</summary>
    public async Task<WebResponseContent> DeleteAsync(JsonElement ids, CancellationToken ct = default)
    {
        if (ids.ValueKind != JsonValueKind.Array || ids.GetArrayLength() == 0)
            return WebResponseContent.Error("请选择要删除的数据");

        var keys = new List<object>();
        foreach (var el in ids.EnumerateArray())
        {
            var k = ParseKey(el);
            if (k != null) keys.Add(k);
        }

        if (keys.Count == 0) return WebResponseContent.Error("主键无效");

        var set = _db.Set<TEntity>();
        var deleted = 0;
        foreach (var key in keys)
        {
            var row = await FindByKeyAsync(key, ct);
            if (row == null) continue;
            if (row is BaseEntity be)
            {
                be.IsDeleted = true;
                be.ModifyDate = DateTime.UtcNow;
            }
            else
            {
                set.Remove(row);
            }

            deleted++;
        }

        await _db.SaveChangesAsync(ct);
        return WebResponseContent.Ok($"已删除 {deleted} 条");
    }

    private async Task<TEntity?> FindByKeyAsync(object key, CancellationToken ct)
    {
        var et = _db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"未映射实体 {typeof(TEntity).Name}");
        var pk = et.FindPrimaryKey()?.Properties.FirstOrDefault()
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} 无主键");
        var prop = typeof(TEntity).GetProperty(pk.Name)
            ?? throw new InvalidOperationException($"主键属性 {pk.Name} 不存在");

        // 避免表达式树开销：按类型分支
        if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
        {
            var id = Convert.ToInt32(key);
            return await setFindInt(id, prop.Name, ct);
        }

        if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
        {
            var id = key is Guid g ? g : Guid.Parse(key.ToString()!);
            return await setFindGuid(id, prop.Name, ct);
        }

        return await set.FindAsync([key], ct);
    }

    private IQueryable<TEntity> Set => _db.Set<TEntity>();
    private DbSet<TEntity> set => _db.Set<TEntity>();

    private Task<TEntity?> setFindInt(int id, string propName, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(e => EF.Property<int>(e, propName) == id, ct);

    private Task<TEntity?> setFindGuid(Guid id, string propName, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, propName) == id, ct);

    private static object? GetKeyValue(TEntity entity)
    {
        var prop = typeof(TEntity).GetProperty("Id");
        return prop?.GetValue(entity);
    }

    private static void EnsureNewGuidKey(TEntity entity)
    {
        var prop = typeof(TEntity).GetProperty("Id");
        if (prop == null || prop.PropertyType != typeof(Guid)) return;
        var val = (Guid)(prop.GetValue(entity) ?? Guid.Empty);
        if (val == Guid.Empty) prop.SetValue(entity, Guid.NewGuid());
    }

    private static object? ParseKey(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.Number when el.TryGetInt32(out var i) => i,
            JsonValueKind.Number when el.TryGetInt64(out var l) => (int)l,
            JsonValueKind.String when Guid.TryParse(el.GetString(), out var g) => g,
            JsonValueKind.String when int.TryParse(el.GetString(), out var i) => i,
            _ => null,
        };
}
