using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Core;
using Seven.Infrastructure.Persistence;

namespace Seven.Builder;

/// <summary>
/// 代码生成器服务（适配 Seven 分层架构）
/// </summary>
public class BuilderService : IBuilderService
{
    private readonly SevenDbContext _db;
    private readonly IConfiguration _config;

    public BuilderService(SevenDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetTableTreeAsync(CancellationToken cancellationToken = default)
    {
        var treeData = await _db.Sys_TableInfos.AsNoTracking()
            .OrderByDescending(t => t.OrderNo)
            .Select(t => new
            {
                id = t.Table_Id,
                pId = t.ParentId ?? 0,
                parentId = t.ParentId ?? 0,
                name = t.ColumnCNName ?? t.TableName,
                orderNo = t.OrderNo ?? 0,
            })
            .ToListAsync(cancellationToken);

        var ids = treeData.Select(t => t.id).ToHashSet();
        var treeList = treeData.Select(t => new
        {
            t.id,
            t.pId,
            t.parentId,
            t.name,
            isParent = ids.Any(id => treeData.Any(x => x.pId == t.id)),
        });

        return WebResponseContent.Ok(data: new
        {
            list = treeList,
            nameSpace = ProjectPath.GetNamespaces(),
        });
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> LoadTableAsync(LoadTableRequest req, CancellationToken cancellationToken = default)
    {
        var tableId = await InitTableAsync(req, cancellationToken);
        var tableInfo = await _db.Sys_TableInfos
            .Include(t => t.TableColumns)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Table_Id == tableId, cancellationToken);

        if (tableInfo?.TableColumns != null)
            tableInfo.TableColumns = tableInfo.TableColumns.OrderByDescending(c => c.OrderNo).ToList();

        return WebResponseContent.Ok(data: tableInfo);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> LoadTableInfoAsync(CancellationToken cancellationToken = default)
    {
        var tables = await _db.Sys_TableInfos.Include(t => t.TableColumns).AsNoTracking().ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: tables);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> SaveAsync(Sys_TableInfo tableInfo, CancellationToken cancellationToken = default)
    {
        if (tableInfo.Table_Id == tableInfo.ParentId)
            return WebResponseContent.Error("父级 id 不能为自己");

        if (tableInfo.TableColumns != null)
        {
            foreach (var col in tableInfo.TableColumns)
                col.TableName = tableInfo.TableName;
        }

        if (tableInfo.Table_Id <= 0)
        {
            tableInfo.CreateDate = DateTime.Now;
            _db.Sys_TableInfos.Add(tableInfo);
        }
        else
        {
            var existing = await _db.Sys_TableInfos
                .Include(t => t.TableColumns)
                .FirstOrDefaultAsync(t => t.Table_Id == tableInfo.Table_Id, cancellationToken);
            if (existing == null) return WebResponseContent.Error("配置不存在");

            existing.ParentId = tableInfo.ParentId;
            existing.TableName = tableInfo.TableName;
            existing.TableTrueName = tableInfo.TableTrueName;
            existing.ColumnCNName = tableInfo.ColumnCNName;
            existing.Namespace = tableInfo.Namespace;
            existing.FolderName = tableInfo.FolderName;
            existing.OrderNo = tableInfo.OrderNo;
            existing.ExpressField = tableInfo.ExpressField;
            existing.CnName = tableInfo.CnName;
            existing.ModifyDate = DateTime.Now;

            if (tableInfo.TableColumns != null)
            {
                _db.Sys_TableColumns.RemoveRange(existing.TableColumns);
                foreach (var col in tableInfo.TableColumns)
                {
                    col.Table_Id = existing.Table_Id;
                    col.ColumnId = 0;
                    _db.Sys_TableColumns.Add(col);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("保存成功", tableInfo);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> SyncTableAsync(string tableName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return WebResponseContent.Error("表名不能为空");

        var tableInfo = await _db.Sys_TableInfos
            .Include(t => t.TableColumns)
            .FirstOrDefaultAsync(t => t.TableName == tableName, cancellationToken);

        if (tableInfo == null)
            return WebResponseContent.Error($"未找到表 {tableName} 的配置，请先新建");

        var realName = string.IsNullOrEmpty(tableInfo.TableTrueName) ? tableName : tableInfo.TableTrueName;
        var dbColumns = await ReadDbColumnsAsync(realName, cancellationToken);
        if (dbColumns.Count == 0)
            return WebResponseContent.Error($"未读取到表 {realName} 的结构");

        var existing = tableInfo.TableColumns.ToList();
        var added = 0;
        var updated = 0;
        var removed = 0;

        foreach (var col in dbColumns)
        {
            var old = existing.FirstOrDefault(c => c.ColumnName.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase));
            if (old == null)
            {
                col.Table_Id = tableInfo.Table_Id;
                col.TableName = tableInfo.TableName;
                _db.Sys_TableColumns.Add(col);
                added++;
            }
            else if (old.ColumnType != col.ColumnType || old.Maxlength != col.Maxlength)
            {
                old.ColumnType = col.ColumnType;
                old.Maxlength = col.Maxlength;
                old.IsNull = col.IsNull;
                updated++;
            }
        }

        foreach (var old in existing.Where(c => !dbColumns.Any(d => d.ColumnName.Equals(c.ColumnName, StringComparison.OrdinalIgnoreCase))))
        {
            _db.Sys_TableColumns.Remove(old);
            removed++;
        }

        if (added + updated + removed == 0)
            return WebResponseContent.Error("表结构未发生变化");

        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok($"新增 {added} 列，更新 {updated} 列，删除 {removed} 列");
    }

    /// <inheritdoc />
    public Task<WebResponseContent> CreateModelAsync(Sys_TableInfo tableInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableInfo.TableName))
            return Task.FromResult(WebResponseContent.Error("表名不能为空"));

        var ns = tableInfo.Namespace ?? "Seven.Domain.Entities.System";
        var folder = tableInfo.FolderName ?? "System";
        var entityName = tableInfo.TableName;
        var sb = new StringBuilder();
        sb.AppendLine("using Seven.Domain.Common;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>{tableInfo.ColumnCNName ?? entityName}</summary>");
        sb.AppendLine($"public class {entityName} : BaseEntity");
        sb.AppendLine("{");

        foreach (var col in (tableInfo.TableColumns ?? []).OrderByDescending(c => c.OrderNo))
        {
            if (col.IsColumnData == 0) continue;
            var clrType = MapClrType(col.ColumnType);
            sb.AppendLine($"    /// <summary>{col.ColumnCNName ?? col.ColumnName}</summary>");
            sb.AppendLine($"    public {clrType} {col.ColumnName} {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        var path = Path.Combine(ProjectPath.DomainPath, folder, $"{entityName}.cs");
        FileHelper.WriteFile(path, sb.ToString());
        return Task.FromResult(WebResponseContent.Ok($"已生成 {path}"));
    }

    /// <inheritdoc />
    public Task<WebResponseContent> CreateServicesAsync(CreateServicesRequest request, CancellationToken cancellationToken = default)
    {
        var tableName = request.TableName;
        var folder = request.FolderName;
        var messages = new List<string>();

        var serviceName = $"Sys{tableName.Replace("Sys_", "").Replace("_", "")}Service";
        var interfaceName = $"I{serviceName}";

        var serviceTemplate = FileHelper.ReadTemplate("ServiceImpl.html");
        var ifaceTemplate = FileHelper.ReadTemplate("ServiceInterface.html");
        var controllerTemplate = FileHelper.ReadTemplate("Controller.html");

        var tokens = new Dictionary<string, string>
        {
            ["TableName"] = tableName,
            ["ServiceName"] = serviceName,
            ["InterfaceName"] = interfaceName,
            ["EntityName"] = tableName,
            ["RouteName"] = tableName,
        };

        if (!string.IsNullOrEmpty(serviceTemplate))
        {
            var path = Path.Combine(ProjectPath.InfrastructurePath, $"{serviceName}.cs");
            FileHelper.WriteFile(path, FileHelper.ReplaceTokens(serviceTemplate, tokens));
            messages.Add(path);
        }

        if (!string.IsNullOrEmpty(ifaceTemplate))
        {
            var path = Path.Combine(ProjectPath.ApplicationPath, "Generated", $"{interfaceName}.cs");
            FileHelper.WriteFile(path, FileHelper.ReplaceTokens(ifaceTemplate, tokens));
            messages.Add(path);
        }

        if (!string.IsNullOrEmpty(controllerTemplate))
        {
            var path = Path.Combine(ProjectPath.WebApiPath, "Generated", $"{tableName}Controller.cs");
            FileHelper.WriteFile(path, FileHelper.ReplaceTokens(controllerTemplate, tokens));
            messages.Add(path);
        }

        return Task.FromResult(WebResponseContent.Ok($"已生成:\n{string.Join("\n", messages)}"));
    }

    /// <inheritdoc />
    public Task<WebResponseContent> CreateVuePageAsync(CreateVuePageRequest request, CancellationToken cancellationToken = default)
    {
        var info = request.TableInfo;
        if (string.IsNullOrWhiteSpace(info.TableName))
            return Task.FromResult(WebResponseContent.Error("表名不能为空"));

        var template = FileHelper.ReadTemplate("VuePage.html");
        if (string.IsNullOrEmpty(template))
            return Task.FromResult(WebResponseContent.Error("Vue 模板不存在"));

        var columns = info.TableColumns?.Where(c => c.IsColumnData != 0).OrderByDescending(c => c.OrderNo).ToList() ?? [];
        var tableCols = string.Join("\n        ", columns.Select(c =>
            $"<el-table-column prop=\"{ToCamelCase(c.ColumnName)}\" label=\"{c.ColumnCNName ?? c.ColumnName}\" />"));

        var formItems = string.Join("\n        ", columns.Where(c => c.Editable && !c.IsKey).Select(c =>
            $"<el-form-item label=\"{c.ColumnCNName ?? c.ColumnName}\"><el-input v-model=\"form.{ToCamelCase(c.ColumnName)}\" /></el-form-item>"));

        var tokens = new Dictionary<string, string>
        {
            ["TableName"] = info.TableName,
            ["Title"] = info.ColumnCNName ?? info.TableName,
            ["ApiRoute"] = info.TableName,
            ["TableColumns"] = tableCols,
            ["FormItems"] = formItems,
            ["FormFields"] = string.Join(", ", columns.Select(c => $"{ToCamelCase(c.ColumnName)}: ''")),
        };

        var vuePath = string.IsNullOrEmpty(request.VuePath)
            ? Path.Combine(ProjectPath.VueViewsPath, $"{info.TableName}.vue")
            : request.VuePath;

        FileHelper.WriteFile(vuePath, FileHelper.ReplaceTokens(template, tokens));
        return Task.FromResult(WebResponseContent.Ok($"已生成 {vuePath}"));
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DelTreeAsync(int tableId, CancellationToken cancellationToken = default)
    {
        if (tableId <= 0) return WebResponseContent.Error("参数无效");

        var tableInfo = await _db.Sys_TableInfos.Include(t => t.TableColumns)
            .FirstOrDefaultAsync(t => t.Table_Id == tableId, cancellationToken);
        if (tableInfo == null) return WebResponseContent.Ok("已删除");

        if (tableInfo.TableColumns.Count > 0)
            return WebResponseContent.Error("当前节点存在表结构，只能删除空节点");

        if (await _db.Sys_TableInfos.AnyAsync(t => t.ParentId == tableId, cancellationToken))
            return WebResponseContent.Error("当前节点存在子节点，不能删除");

        _db.Sys_TableInfos.Remove(tableInfo);
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    async Task<int> InitTableAsync(LoadTableRequest req, CancellationToken cancellationToken)
    {
        if (req.Table_Id > 0 && !req.IsTreeLoad)
            return req.Table_Id;

        if (req.IsTreeLoad && req.Table_Id > 0)
            return req.Table_Id;

        Sys_TableInfo? existing = null;
        if (req.Table_Id > 0)
            existing = await _db.Sys_TableInfos.FirstOrDefaultAsync(t => t.Table_Id == req.Table_Id, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(req.TableName))
            existing = await _db.Sys_TableInfos.FirstOrDefaultAsync(t => t.TableName == req.TableName, cancellationToken);

        if (existing != null)
        {
            if (existing.TableColumns.Count == 0 && !string.IsNullOrWhiteSpace(existing.TableName))
            {
                var cols = await ReadDbColumnsAsync(existing.TableName, cancellationToken);
                if (cols.Count > 0)
                {
                    foreach (var c in cols) { c.Table_Id = existing.Table_Id; c.TableName = existing.TableName; }
                    _db.Sys_TableColumns.AddRange(cols);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            return existing.Table_Id;
        }

        var table = new Sys_TableInfo
        {
            ParentId = req.ParentId,
            TableName = req.TableName,
            ColumnCNName = req.ColumnCNName,
            Namespace = req.Namespace,
            FolderName = req.FolderName,
            OrderNo = 0,
            Enable = 1,
            CreateDate = DateTime.Now,
        };
        _db.Sys_TableInfos.Add(table);
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(req.TableName))
        {
            var cols = await ReadDbColumnsAsync(req.TableName, cancellationToken);
            foreach (var c in cols) { c.Table_Id = table.Table_Id; c.TableName = table.TableName; }
            if (cols.Count > 0)
            {
                _db.Sys_TableColumns.AddRange(cols);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return table.Table_Id;
    }

    async Task<List<Sys_TableColumn>> ReadDbColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var provider = _db.Database.ProviderName ?? "";
        if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return await ReadMySqlColumnsAsync(tableName, cancellationToken);

        return ReadEfColumns(tableName);
    }

    async Task<List<Sys_TableColumn>> ReadMySqlColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var connStr = _config.GetConnectionString("Default") ?? "";
        var dbName = ExtractDatabaseName(connStr);
        var sql = """
            SELECT COLUMN_NAME AS ColumnName, DATA_TYPE AS ColumnType,
                   CHARACTER_MAXIMUM_LENGTH AS MaxLen,
                   CASE WHEN COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END AS IsPri,
                   CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNull
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = @db AND TABLE_NAME = @table
            ORDER BY ORDINAL_POSITION
            """;

        var result = new List<Sys_TableColumn>();
        await using var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var pDb = cmd.CreateParameter();
        pDb.ParameterName = "@db";
        pDb.Value = dbName;
        cmd.Parameters.Add(pDb);
        var pTable = cmd.CreateParameter();
        pTable.ParameterName = "@table";
        pTable.Value = tableName;
        cmd.Parameters.Add(pTable);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var order = 100;
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new Sys_TableColumn
            {
                ColumnName = reader.GetString(0),
                ColumnCNName = reader.GetString(0),
                ColumnType = reader.GetString(1),
                Maxlength = reader.IsDBNull(2) ? null : Convert.ToInt32(reader.GetValue(2)),
                IsKey = reader.GetInt32(3) == 1,
                IsNull = reader.GetInt32(4),
                Editable = reader.GetInt32(3) != 1,
                Enable = 1,
                IsDisplay = 1,
                IsColumnData = 1,
                OrderNo = order--,
            });
        }
        return result;
    }

    List<Sys_TableColumn> ReadEfColumns(string tableName)
    {
        var entityType = _db.Model.GetEntityTypes()
            .FirstOrDefault(e => e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true);
        if (entityType == null) return [];

        return entityType.GetProperties().Select((p, i) => new Sys_TableColumn
        {
            ColumnName = p.Name,
            ColumnCNName = p.Name,
            ColumnType = p.GetColumnType(),
            IsKey = p.IsPrimaryKey(),
            Editable = !p.IsPrimaryKey(),
            Enable = 1,
            IsDisplay = 1,
            IsColumnData = 1,
            OrderNo = 100 - i,
        }).ToList();
    }

    static string ExtractDatabaseName(string connStr)
    {
        foreach (var part in connStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }
        return "seven";
    }

    static string MapClrType(string? dbType)
    {
        var t = (dbType ?? "nvarchar").ToLowerInvariant();
        if (t is "int" or "integer" or "mediumint") return "int";
        if (t is "bigint") return "long";
        if (t is "tinyint" or "smallint") return "int";
        if (t is "bit" or "bool" or "boolean") return "bool";
        if (t is "decimal" or "numeric" or "float" or "double") return "decimal";
        if (t is "datetime" or "timestamp" or "date") return "DateTime?";
        return "string?";
    }

    static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Contains('_'))
        {
            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            return parts[0].ToLowerInvariant() + string.Concat(parts.Skip(1).Select(p =>
                char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        }
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
