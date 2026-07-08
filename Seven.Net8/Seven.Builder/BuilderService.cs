using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Board;
using Seven.Domain.Entities.Core;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Builder;

/// <summary>
/// 代码生成器服务
/// </summary>
public class BuilderService : IBuilderService
{
    private readonly SevenDbContext _db;

    /// <summary>构造函数</summary>
    public BuilderService(SevenDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<WebResponseContent> LoadTableInfoAsync(CancellationToken cancellationToken = default)
    {
        var tables = await _db.Sys_TableInfos.Include(t => t.TableColumns).AsNoTracking().ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: tables);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> SyncTableAsync(string tableName, CancellationToken cancellationToken = default)
    {
        // 从 EF 模型元数据同步表结构到 Sys_TableInfo
        var entityType = _db.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true);
        if (entityType == null) return WebResponseContent.Error($"表 {tableName} 不存在于 EF 模型中");

        var existing = await _db.Sys_TableInfos.FirstOrDefaultAsync(t => t.TableName == tableName, cancellationToken);
        if (existing == null)
        {
            existing = new Sys_TableInfo { TableName = tableName, ColumnCNName = tableName, CreateDate = DateTime.Now };
            _db.Sys_TableInfos.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var columns = entityType.GetProperties().Select((p, i) => new Sys_TableColumn
        {
            Table_Id = existing.Table_Id,
            ColumnName = p.Name,
            ColumnCNName = p.Name,
            ColumnType = p.GetColumnType(),
            IsKey = p.IsPrimaryKey(),
            Editable = true
        }).ToList();

        var oldCols = await _db.Sys_TableColumns.Where(c => c.Table_Id == existing.Table_Id).ToListAsync(cancellationToken);
        _db.Sys_TableColumns.RemoveRange(oldCols);
        _db.Sys_TableColumns.AddRange(columns);
        await _db.SaveChangesAsync(cancellationToken);

        return WebResponseContent.Ok($"表 {tableName} 同步成功", existing);
    }
}
