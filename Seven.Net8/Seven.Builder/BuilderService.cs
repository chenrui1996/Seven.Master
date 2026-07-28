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

        // 配置表名 / 真实表名 均按忽略大小写匹配
        var tableInfo = await _db.Sys_TableInfos
            .Include(t => t.TableColumns)
            .FirstOrDefaultAsync(t =>
                (t.TableName != null && t.TableName.ToLower() == tableName.ToLower())
                || (t.TableTrueName != null && t.TableTrueName.ToLower() == tableName.ToLower()),
                cancellationToken);

        // 无代码生成配置时：若物理表存在则自动建一条配置再同步（避免「库里有表却提示先新建」）
        if (tableInfo == null)
        {
            var physicalName = ResolvePhysicalTableNameByRequest(tableName);
            var bootstrapCols = await ReadDbColumnsAsync(physicalName, cancellationToken);
            if (bootstrapCols.Count == 0)
            {
                var dbName = _db.Database.GetDbConnection().Database ?? ResolveMySqlDatabaseName();
                return WebResponseContent.Error(
                    $"未找到表 [{tableName}] 的代码生成配置，且数据库 [{dbName}] 中也读不到物理表 [{physicalName}] 的结构。" +
                    "请先在代码生成页新建配置，或确认表名/库名正确（EF 映射为 Device）。");
            }

            tableInfo = new Sys_TableInfo
            {
                TableName = physicalName,
                TableTrueName = physicalName,
                ColumnCNName = physicalName,
                Namespace = "Seven.Domain.Entities.Board",
                FolderName = "Board",
                Enable = 1,
                OrderNo = 0,
                CreateDate = DateTime.Now,
            };
            _db.Sys_TableInfos.Add(tableInfo);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var col in bootstrapCols)
            {
                col.Table_Id = tableInfo.Table_Id;
                col.TableName = tableInfo.TableName;
                _db.Sys_TableColumns.Add(col);
            }
            await _db.SaveChangesAsync(cancellationToken);
            return WebResponseContent.Ok($"已自动创建配置并同步 {bootstrapCols.Count} 列");
        }

        var realName = ResolvePhysicalTableName(tableInfo, tableName);
        var dbColumns = await ReadDbColumnsAsync(realName, cancellationToken);
        if (dbColumns.Count == 0)
        {
            var dbName = _db.Database.GetDbConnection().Database
                ?? ResolveMySqlDatabaseName();
            return WebResponseContent.Error(
                $"未读取到表结构。当前库=[{dbName}]，解析表名=[{realName}]，配置表名=[{tableInfo.TableName}]，真实表名=[{tableInfo.TableTrueName}]。" +
                "请确认表在同一数据库，且 TableTrueName 填物理表名（如 Device）。");
        }

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
    public async Task<WebResponseContent> CreateServicesAsync(CreateServicesRequest request, CancellationToken cancellationToken = default)
    {
        var tableName = request.TableName;
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(tableName))
            return WebResponseContent.Error("表名不能为空");

        var entityNamespace = string.IsNullOrWhiteSpace(request.Namespace)
            ? "Seven.Domain.Entities.System"
            : request.Namespace.Trim();

        var serviceName = $"Sys{tableName.Replace("Sys_", "").Replace("_", "")}Service";
        var interfaceName = $"I{serviceName}";
        var keyName = await ResolveKeyPropertyNameAsync(tableName, cancellationToken);

        var serviceTemplate = FileHelper.ReadTemplate("ServiceImpl.html");
        var ifaceTemplate = FileHelper.ReadTemplate("ServiceInterface.html");
        var controllerTemplate = FileHelper.ReadTemplate("Controller.html");

        var tokens = new Dictionary<string, string>
        {
            ["TableName"] = tableName,
            ["ServiceName"] = serviceName,
            ["InterfaceName"] = interfaceName,
            ["EntityName"] = tableName,
            ["EntityNamespace"] = entityNamespace,
            ["RouteName"] = tableName,
            ["KeyName"] = keyName,
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

        return WebResponseContent.Ok($"已生成:\n{string.Join("\n", messages)}\n请重启 WebApi 使 DI/路由生效");
    }

    async Task<string> ResolveKeyPropertyNameAsync(string tableName, CancellationToken cancellationToken)
    {
        var col = await _db.Sys_TableColumns.AsNoTracking()
            .Where(c => c.TableName == tableName && c.IsKey)
            .Select(c => c.ColumnName)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(col)) return col;

        var efPk = _db.Model.GetEntityTypes()
            .FirstOrDefault(e =>
                e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true
                || e.ClrType.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            ?.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
        if (!string.IsNullOrWhiteSpace(efPk)) return efPk;

        return $"{tableName}Id";
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

        var allColumns = info.TableColumns?
            .Where(c => c.IsColumnData != 0 && c.IsDisplay != 0)
            .OrderByDescending(c => c.OrderNo)
            .ToList() ?? [];

        var keyCol = info.TableColumns?.FirstOrDefault(c => c.IsKey)?.ColumnName
            ?? $"{info.TableName}Id";
        var keyFieldCamel = ToCamelCase(keyCol);
        var entityType = ResolveEntityClrType(info);

        var displayColumns = allColumns
            .Where(c => !IsAuditField(c.ColumnName) || c.IsKey)
            .Select(c => BuildColumnUi(c, entityType))
            .ToList();
        if (displayColumns.Count == 0)
            displayColumns = allColumns.Select(c => BuildColumnUi(c, entityType)).ToList();

        // 表单：非主键、非审计；主键不参与新增表单
        var formColumns = allColumns
            .Where(c => !c.IsKey && c.Editable && !IsAuditField(c.ColumnName))
            .Select(c => BuildColumnUi(c, entityType))
            .ToList();

        var i18nKey = $"generated.{SanitizeI18nKey(info.TableName)}";
        var columnDefs = BuildColumnDefsScript(displayColumns);
        var formItems = string.Join("\n        ", formColumns.Select(ui => BuildFormItemMarkup(ui, i18nKey)));

        // 表单状态含主键（新增为 0），不含审计字段
        var formStateColumns = new List<ColumnUiMeta>();
        var keyUi = BuildColumnUi(
            info.TableColumns?.FirstOrDefault(c => c.IsKey)
            ?? new Sys_TableColumn { ColumnName = keyCol, ColumnType = "int", IsKey = true },
            entityType);
        formStateColumns.Add(keyUi);
        formStateColumns.AddRange(formColumns);

        var formFields = string.Join(", ", formStateColumns.Select(ui => $"{ui.Prop}: {ui.DefaultLiteral}"));
        var enumOptions = BuildEnumOptionsScript(formColumns.Concat(displayColumns).DistinctBy(x => x.Prop).ToList());

        var folder = ProjectPath.ResolveVueFolder(info.FolderName, info.Namespace);
        var folderNorm = folder.Replace('\\', '/');
        var depthPrefix = folderNorm.Contains('/') ? "../../../" : "../../";
        var httpImport = $"{depthPrefix}api/http";
        var extensionImport = $"{depthPrefix}extension/{folderNorm}/{info.TableName}";
        var extensionTypesImport = $"{depthPrefix}extension/types";
        var userStoreImport = $"{depthPrefix}stores/user";
        var actionIconsImport = $"{depthPrefix}constants/actionIcons";
        var componentsImport = $"{depthPrefix}components";
        var composablesImport = $"{depthPrefix}composables";

        var tokens = new Dictionary<string, string>
        {
            ["TableName"] = info.TableName,
            ["Title"] = ToChineseLabel(info.ColumnCNName, info.TableName),
            ["I18nKey"] = i18nKey,
            ["ApiRoute"] = info.TableName,
            ["ColumnDefs"] = columnDefs,
            ["FormItems"] = formItems,
            ["FormFields"] = formFields,
            ["HttpImport"] = httpImport,
            ["ExtensionImport"] = extensionImport,
            ["ExtensionTypesImport"] = extensionTypesImport,
            ["UserStoreImport"] = userStoreImport,
            ["ActionIconsImport"] = actionIconsImport,
            ["ComponentsImport"] = componentsImport,
            ["ComposablesImport"] = composablesImport,
            ["KeyFieldCamel"] = keyFieldCamel,
            ["EnumOptions"] = string.IsNullOrWhiteSpace(enumOptions) ? "" : enumOptions + Environment.NewLine,
        };

        var vuePath = string.IsNullOrEmpty(request.VuePath)
            ? Path.Combine(ProjectPath.VueViewsPath, folder, $"{info.TableName}.vue")
            : request.VuePath;

        var content = ApplyVuePageTokens(template, tokens);
        // 再走一遍 FileHelper，保证 /*__ENUM_OPTIONS__*/ 等脚本占位必被处理
        content = FileHelper.ReplaceTokens(content, tokens);
        // 显式覆盖 import 路径：避免旧进程/漏配 token 时写出 {{ActionIconsImport}} 等坏文件
        content = ForceReplaceImportTokens(
            content,
            httpImport,
            extensionImport,
            extensionTypesImport,
            userStoreImport,
            actionIconsImport,
            componentsImport,
            composablesImport);

        if (ContainsUnreplacedCodegenToken(content))
            return Task.FromResult(WebResponseContent.Error(
                "Vue 生成失败：模板占位符未替换完整。请重新编译并重启 WebApi 后再生成。"));

        FileHelper.WriteFile(vuePath, content);

        var extensionPath = Path.Combine(ProjectPath.VueExtensionPath, folder, $"{info.TableName}.ts");
        var extensionNote = EnsureVueExtensionFile(extensionPath, info.TableName);

        MergeGeneratedLocales(info, allColumns, displayColumns.Concat(formColumns));

        return Task.FromResult(WebResponseContent.Ok(
            $"已生成 {vuePath}，并写入多语言词条 {i18nKey}。{extensionNote}"));
    }

    /// <summary>首次创建扩展文件；已存在则跳过（永不覆盖业务扩展）</summary>
    static string EnsureVueExtensionFile(string extensionPath, string tableName)
    {
        if (File.Exists(extensionPath))
            return $"已保留扩展文件 {extensionPath}";

        var extTemplate = FileHelper.ReadTemplate("VueExtension.ts.html");
        if (string.IsNullOrEmpty(extTemplate))
        {
            // 模板缺失时写最小可用扩展，避免生成页 import 失败
            extTemplate = """
import type { PageExtension } from '../types'

const extension: PageExtension = {
  toolbarButtons: [],
  rowButtons: [],
}

export default extension
""";
        }

        var body = FileHelper.ReplaceTokens(extTemplate, new Dictionary<string, string>
        {
            ["TableName"] = tableName,
        });
        FileHelper.WriteFile(extensionPath, body);
        return $"已创建扩展文件 {extensionPath}（可在此添加自定义按钮）";
    }

    static string ForceReplaceImportTokens(
        string content,
        string httpImport,
        string extensionImport,
        string extensionTypesImport,
        string userStoreImport,
        string actionIconsImport,
        string componentsImport,
        string composablesImport)
    {
        (string key, string value)[] imports =
        [
            ("HttpImport", httpImport),
            ("ExtensionImport", extensionImport),
            ("ExtensionTypesImport", extensionTypesImport),
            ("UserStoreImport", userStoreImport),
            ("ActionIconsImport", actionIconsImport),
            ("ComponentsImport", componentsImport),
            ("ComposablesImport", composablesImport),
        ];
        foreach (var (key, value) in imports)
        {
            content = content.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
            content = content.Replace("__" + key + "__", value, StringComparison.Ordinal);
        }
        return content;
    }

    /// <summary>Vue 页占位符替换（同时支持 {{Key}} / __Key__）</summary>
    static string ApplyVuePageTokens(string template, Dictionary<string, string> tokens)
    {
        var result = template;
        foreach (var key in new[]
                 {
                     "TableColumns", "ColumnDefs", "FormItems", "FormFields", "EnumOptions",
                     "HttpImport", "ExtensionImport", "ExtensionTypesImport", "UserStoreImport",
                     "ActionIconsImport", "ComponentsImport", "ComposablesImport",
                     "I18nKey", "KeyFieldCamel", "ApiRoute", "TableName", "Title",
                 })
        {
            if (!tokens.TryGetValue(key, out var value)) continue;
            result = result.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
            result = result.Replace("__" + key + "__", value, StringComparison.Ordinal);
        }
        foreach (var (key, value) in tokens)
        {
            result = result.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
            result = result.Replace("__" + key + "__", value, StringComparison.Ordinal);
        }

        var enumBlock = tokens.TryGetValue("EnumOptions", out var eo) ? eo ?? "" : "";
        result = result
            .Replace("/*__ENUM_OPTIONS__*/", enumBlock, StringComparison.Ordinal)
            .Replace("{{EnumOptions}}", enumBlock, StringComparison.Ordinal)
            .Replace("__EnumOptions__", enumBlock, StringComparison.Ordinal);
        return result;
    }

    static bool ContainsUnreplacedCodegenToken(string content)
    {
        // 生成后不应再出现代码生成占位符；Vue 插值如 {{ enumLabel(...) }} / {{ t('...') }} 允许保留
        // {{EnumOptions}} 绝不能残留：在 <script> 中会被解析为对变量 EnumOptions 的引用
        string[] forbidden =
        [
            "{{TableColumns}}", "{{ColumnDefs}}", "{{FormItems}}", "{{FormFields}}", "{{EnumOptions}}",
            "{{HttpImport}}", "{{ExtensionImport}}", "{{ExtensionTypesImport}}", "{{UserStoreImport}}",
            "{{ActionIconsImport}}", "{{ComponentsImport}}", "{{ComposablesImport}}",
            "{{I18nKey}}", "{{KeyFieldCamel}}", "{{ApiRoute}}", "{{TableName}}",
            "__TableColumns__", "__ColumnDefs__", "__FormItems__", "__FormFields__", "__EnumOptions__",
            "__HttpImport__", "__ExtensionImport__", "__ExtensionTypesImport__", "__UserStoreImport__",
            "__ActionIconsImport__", "__ComponentsImport__", "__ComposablesImport__",
            "__I18nKey__", "__KeyFieldCamel__", "__ApiRoute__", "__TableName__",
            "/*__ENUM_OPTIONS__*/",
        ];
        return forbidden.Any(content.Contains);
    }

    static void MergeGeneratedLocales(Sys_TableInfo info, List<Sys_TableColumn> columns, IEnumerable<ColumnUiMeta> uiMetas)
    {
        var pageKey = SanitizeI18nKey(info.TableName);
        var zh = BuildLocaleEntries(info, columns, "zh");
        var en = BuildLocaleEntries(info, columns, "en");
        var ja = BuildLocaleEntries(info, columns, "ja");

        foreach (var ui in uiMetas.Where(u => u.Kind == ColumnUiKind.Enum && u.EnumOptions.Count > 0))
        {
            foreach (var opt in ui.EnumOptions)
            {
                zh[$"enum_{ui.Prop}_{opt.Value}"] = opt.LabelZh;
                en[$"enum_{ui.Prop}_{opt.Value}"] = opt.LabelEn;
                ja[$"enum_{ui.Prop}_{opt.Value}"] = opt.LabelZh;
            }
        }

        // 操作列文案强制写入，避免旧进程/覆盖导致 deleteConfirm 缺失
        EnsureActionLocaleKeys(zh, "zh");
        EnsureActionLocaleKeys(en, "en");
        EnsureActionLocaleKeys(ja, "ja");

        MergeLocaleFile(Path.Combine(ProjectPath.VueLocalesPath, "zh-CN.json"), pageKey, zh);
        MergeLocaleFile(Path.Combine(ProjectPath.VueLocalesPath, "en-US.json"), pageKey, en);
        MergeLocaleFile(Path.Combine(ProjectPath.VueLocalesPath, "ja-JP.json"), pageKey, ja);
    }

    static void EnsureActionLocaleKeys(Dictionary<string, string> dict, string lang)
    {
        dict["actions"] = lang switch { "en" => "Actions", "ja" => "操作", _ => "操作" };
        dict["edit"] = lang switch { "en" => "Edit", "ja" => "編集", _ => "编辑" };
        dict["add"] = lang switch { "en" => "Add", "ja" => "追加", _ => "新增" };
        dict["delete"] = lang switch { "en" => "Delete", "ja" => "削除", _ => "删除" };
        dict["deleteConfirm"] = lang switch
        {
            "en" => "Are you sure you want to delete this record?",
            "ja" => "このレコードを削除しますか？",
            _ => "确定删除该记录吗？",
        };
    }

    Type? ResolveEntityClrType(Sys_TableInfo info)
    {
        var ns = info.Namespace?.Trim();
        var name = info.TableName?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return null;

        var domainAsm = typeof(Seven.Domain.Common.BaseEntity).Assembly;
        if (!string.IsNullOrWhiteSpace(ns))
        {
            var full = domainAsm.GetType($"{ns}.{name}", throwOnError: false, ignoreCase: true);
            if (full != null) return full;
        }

        return domainAsm.GetTypes().FirstOrDefault(t =>
            t.IsClass && !t.IsAbstract && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    ColumnUiMeta BuildColumnUi(Sys_TableColumn column, Type? entityType)
    {
        var propName = column.ColumnName;
        var prop = entityType?.GetProperty(propName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        var clr = Nullable.GetUnderlyingType(prop?.PropertyType ?? typeof(string)) ?? prop?.PropertyType;
        var formEnum = prop?.GetCustomAttributes(typeof(Seven.Domain.Attributes.FormEnumAttribute), false)
            .OfType<Seven.Domain.Attributes.FormEnumAttribute>()
            .FirstOrDefault();

        var kind = ColumnUiKind.String;
        Type? enumType = null;
        if (column.IsKey)
            kind = ColumnUiKind.Key;
        else if (formEnum != null)
        {
            kind = ColumnUiKind.Enum;
            enumType = formEnum.EnumType;
        }
        else if (clr?.IsEnum == true)
        {
            kind = ColumnUiKind.Enum;
            enumType = clr;
        }
        else if (!string.IsNullOrWhiteSpace(column.DropNo) ||
                 string.Equals(column.EditType, "select", StringComparison.OrdinalIgnoreCase))
            kind = ColumnUiKind.Select;
        else if (clr != null)
            kind = MapClrToUiKind(clr);
        else
            kind = MapDbTypeToUiKind(column.ColumnType);

        var meta = new ColumnUiMeta
        {
            Prop = ToCamelCase(propName),
            ColumnName = propName,
            Kind = kind,
            IsKey = column.IsKey,
            Sortable = (column.Sortable ?? 0) == 1,
            IsDecimal = kind == ColumnUiKind.Number && IsDecimalClr(clr, column.ColumnType),
        };

        if (kind == ColumnUiKind.Enum && enumType != null)
            meta.EnumOptions = BuildEnumOptions(enumType);

        meta.DefaultLiteral = kind switch
        {
            ColumnUiKind.Bool => "false",
            ColumnUiKind.Number or ColumnUiKind.Enum or ColumnUiKind.Key or ColumnUiKind.Select => "0",
            ColumnUiKind.Date => "null",
            _ => "''",
        };
        return meta;
    }

    static ColumnUiKind MapClrToUiKind(Type clr)
    {
        if (clr == typeof(bool)) return ColumnUiKind.Bool;
        if (clr == typeof(DateTime) || clr == typeof(DateTimeOffset) || clr == typeof(DateOnly) || clr == typeof(TimeOnly))
            return ColumnUiKind.Date;
        if (clr == typeof(byte) || clr == typeof(short) || clr == typeof(int) || clr == typeof(long)
            || clr == typeof(float) || clr == typeof(double) || clr == typeof(decimal))
            return ColumnUiKind.Number;
        return ColumnUiKind.String;
    }

    static ColumnUiKind MapDbTypeToUiKind(string? columnType)
    {
        var type = (columnType ?? string.Empty).ToLowerInvariant();
        if (IsBoolType(type)) return ColumnUiKind.Bool;
        if (IsDateType(type)) return ColumnUiKind.Date;
        if (IsNumberType(type)) return ColumnUiKind.Number;
        return ColumnUiKind.String;
    }

    static bool IsDecimalClr(Type? clr, string? dbType)
    {
        if (clr == typeof(decimal) || clr == typeof(float) || clr == typeof(double)) return true;
        var t = (dbType ?? "").ToLowerInvariant();
        return t is "decimal" or "numeric" or "float" or "double" or "real";
    }

    static List<EnumOptionMeta> BuildEnumOptions(Type enumType)
    {
        var list = new List<EnumOptionMeta>();
        foreach (var name in Enum.GetNames(enumType))
        {
            var member = enumType.GetField(name);
            var raw = Convert.ToInt32(Enum.Parse(enumType, name));
            var desc = member?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .OfType<System.ComponentModel.DescriptionAttribute>()
                .FirstOrDefault()?.Description;
            var zh = !string.IsNullOrWhiteSpace(desc) ? desc! : ToChineseLabel(null, name);
            list.Add(new EnumOptionMeta
            {
                Value = raw,
                Name = name,
                LabelZh = zh,
                LabelEn = name,
            });
        }
        return list;
    }

    static string BuildColumnDefsScript(List<ColumnUiMeta> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");
        foreach (var ui in columns)
        {
            var kind = ui.Kind switch
            {
                ColumnUiKind.Enum => "enum",
                ColumnUiKind.Bool => "bool",
                ColumnUiKind.Date => "date",
                ColumnUiKind.Number or ColumnUiKind.Key => "number",
                _ => "string",
            };
            var sortable = ui.Sortable ? "true" : "false";
            sb.AppendLine($"  {{ prop: '{ui.Prop}', kind: '{kind}', sortable: {sortable} }},");
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string BuildTableColumnMarkup(ColumnUiMeta ui, string i18nKey)
    {
        var label = $":label=\"t('{i18nKey}.{ui.Prop}')\"";
        var sortable = ui.Sortable ? " sortable=\"custom\"" : "";
        return ui.Kind switch
        {
            ColumnUiKind.Enum =>
                $"<el-table-column {label}{sortable}>\n          <template #default=\"{{ row }}\">{{{{ enumLabel({ui.Prop}Options, row.{ui.Prop}) }}}}</template>\n        </el-table-column>",
            ColumnUiKind.Bool =>
                $"<el-table-column {label}{sortable}>\n          <template #default=\"{{ row }}\">{{{{ row.{ui.Prop} ? t('common.enabled') : t('common.disabled') }}}}</template>\n        </el-table-column>",
            ColumnUiKind.Date =>
                $"<el-table-column {label}{sortable}>\n          <template #default=\"{{ row }}\">{{{{ formatDate(row.{ui.Prop}) }}}}</template>\n        </el-table-column>",
            ColumnUiKind.Number =>
                $"<el-table-column prop=\"{ui.Prop}\" {label}{sortable} align=\"right\" />",
            _ =>
                $"<el-table-column prop=\"{ui.Prop}\" {label}{sortable} />",
        };
    }

    static string BuildFormItemMarkup(ColumnUiMeta ui, string i18nKey)
    {
        var control = BuildFormControlMarkup(ui);
        // 复杂控件与 Device 示例页对齐：标签与控件分行
        if (ui.Kind is ColumnUiKind.Enum or ColumnUiKind.Select or ColumnUiKind.Date)
            return $"<el-form-item :label=\"t('{i18nKey}.{ui.Prop}')\">\n          {control}\n        </el-form-item>";
        return $"<el-form-item :label=\"t('{i18nKey}.{ui.Prop}')\">{control}</el-form-item>";
    }

    static string BuildFormControlMarkup(ColumnUiMeta ui)
    {
        return ui.Kind switch
        {
            ColumnUiKind.Enum =>
                $"<el-select v-model=\"form.{ui.Prop}\" style=\"width:100%\" clearable>\n            <el-option v-for=\"o in {ui.Prop}Options\" :key=\"o.value\" :label=\"o.label\" :value=\"o.value\" />\n          </el-select>",
            ColumnUiKind.Select =>
                $"<el-select v-model=\"form.{ui.Prop}\" style=\"width:100%\" clearable filterable>\n            <el-option v-for=\"o in {ui.Prop}Options\" :key=\"o.value\" :label=\"o.label\" :value=\"o.value\" />\n          </el-select>",
            ColumnUiKind.Bool =>
                $"<el-switch v-model=\"form.{ui.Prop}\" />",
            ColumnUiKind.Date =>
                $"<el-date-picker v-model=\"form.{ui.Prop}\" type=\"datetime\" value-format=\"YYYY-MM-DDTHH:mm:ss\" style=\"width:100%\" clearable />",
            ColumnUiKind.Number when ui.IsDecimal =>
                $"<el-input-number v-model=\"form.{ui.Prop}\" :precision=\"2\" :step=\"0.01\" controls-position=\"right\" style=\"width:100%\" />",
            ColumnUiKind.Number or ColumnUiKind.Key =>
                $"<el-input-number v-model=\"form.{ui.Prop}\" :precision=\"0\" :step=\"1\" controls-position=\"right\" style=\"width:100%\" />",
            _ when ui.IsLongText =>
                $"<el-input v-model=\"form.{ui.Prop}\" type=\"textarea\" :rows=\"3\" />",
            _ =>
                $"<el-input v-model=\"form.{ui.Prop}\" clearable />",
        };
    }

    /// <summary>仅生成枚举/下拉 options；formatDate/enumLabel 已写在 VuePage 模板中</summary>
    static string BuildEnumOptionsScript(List<ColumnUiMeta> columns)
    {
        var sb = new StringBuilder();
        var optionCols = columns.Where(c => c.Kind is ColumnUiKind.Enum or ColumnUiKind.Select).ToList();
        foreach (var ui in optionCols)
        {
            if (ui.EnumOptions.Count == 0)
            {
                sb.AppendLine($"const {ui.Prop}Options: {{ value: number | string; label: string }}[] = []");
                continue;
            }

            sb.AppendLine($"const {ui.Prop}Options = [");
            foreach (var opt in ui.EnumOptions)
            {
                var label = opt.LabelZh.Replace("\\", "\\\\").Replace("'", "\\'");
                sb.AppendLine($"  {{ value: {opt.Value}, label: '{label}' }},");
            }
            sb.AppendLine("]");
            sb.AppendLine();
        }

        sb.AppendLine("const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {");
        foreach (var ui in optionCols)
            sb.AppendLine($"  {ui.Prop}: {ui.Prop}Options,");
        sb.AppendLine("}");

        return sb.ToString().TrimEnd();
    }

    enum ColumnUiKind { Key, String, Number, Bool, Date, Enum, Select }

    sealed class EnumOptionMeta
    {
        public int Value { get; set; }
        public string Name { get; set; } = "";
        public string LabelZh { get; set; } = "";
        public string LabelEn { get; set; } = "";
    }

    sealed class ColumnUiMeta
    {
        public string Prop { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public ColumnUiKind Kind { get; set; }
        public bool IsKey { get; set; }
        public bool Sortable { get; set; }
        public bool IsDecimal { get; set; }
        public bool IsLongText { get; set; }
        public string DefaultLiteral { get; set; } = "''";
        public List<EnumOptionMeta> EnumOptions { get; set; } = [];
    }

    static Dictionary<string, string> BuildLocaleEntries(Sys_TableInfo info, List<Sys_TableColumn> columns, string lang)
    {
        var zhTitle = ToChineseLabel(info.ColumnCNName, info.TableName);
        var dict = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["title"] = lang switch
            {
                "en" => info.TableName,
                "ja" => zhTitle,
                _ => zhTitle,
            },
            ["listTitle"] = lang switch
            {
                "en" => $"{info.TableName} List",
                "ja" => $"{zhTitle}一覧",
                _ => zhTitle,
            },
            ["actions"] = lang switch { "en" => "Actions", "ja" => "操作", _ => "操作" },
            ["edit"] = lang switch { "en" => "Edit", "ja" => "編集", _ => "编辑" },
            ["add"] = lang switch { "en" => "Add", "ja" => "追加", _ => "新增" },
            ["delete"] = lang switch { "en" => "Delete", "ja" => "削除", _ => "删除" },
            ["deleteConfirm"] = lang switch
            {
                "en" => "Are you sure you want to delete this record?",
                "ja" => "このレコードを削除しますか？",
                _ => "确定删除该记录吗？",
            },
        };

        foreach (var c in columns)
        {
            var key = ToCamelCase(c.ColumnName);
            dict[key] = lang switch
            {
                "en" => c.ColumnName,
                "ja" => ToChineseLabel(c.ColumnCNName, c.ColumnName),
                _ => ToChineseLabel(c.ColumnCNName, c.ColumnName),
            };
        }
        return dict;
    }

    static bool IsAuditField(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Equals("CreateId", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Creator", StringComparison.OrdinalIgnoreCase)
            || name.Equals("CreateDate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ModifyId", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Modifier", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ModifyDate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsNumberType(string type) =>
        type is "int" or "integer" or "bigint" or "smallint" or "tinyint" or "mediumint"
            or "decimal" or "numeric" or "float" or "double" or "real"
        || type.Contains("int", StringComparison.Ordinal);

    static bool IsBoolType(string type) =>
        type is "bit" or "bool" or "boolean" || type is "tinyint(1)";

    static bool IsDateType(string type) =>
        type.Contains("date", StringComparison.Ordinal) || type.Contains("time", StringComparison.Ordinal);

    static bool IsLongTextType(string type, int? maxLength) =>
        type.Contains("text", StringComparison.Ordinal) || (maxLength is > 200);

    /// <summary>中文标签：已有中文优先，否则按英文字段名翻译</summary>
    static string ToChineseLabel(string? columnCNName, string columnName)
    {
        if (!string.IsNullOrWhiteSpace(columnCNName) && ContainsCjk(columnCNName))
            return columnCNName.Trim();

        var source = string.IsNullOrWhiteSpace(columnCNName) ? columnName : columnCNName.Trim();
        if (ExactZhLabels.TryGetValue(source, out var exact))
            return exact;
        if (ExactZhLabels.TryGetValue(columnName, out exact))
            return exact;

        return TranslatePascalCase(columnName);
    }

    static bool ContainsCjk(string text) => text.Any(c => c is >= '\u4e00' and <= '\u9fff');

    static string TranslatePascalCase(string name)
    {
        var parts = System.Text.RegularExpressions.Regex.Split(name, "(?<=[a-z0-9])(?=[A-Z])|_+")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
        if (parts.Length == 0) return name;

        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (WordZhLabels.TryGetValue(part, out var zh))
                sb.Append(zh);
            else if (ExactZhLabels.TryGetValue(part, out zh))
                sb.Append(zh);
            else
                sb.Append(part);
        }
        return sb.Length > 0 ? sb.ToString() : name;
    }

    static readonly Dictionary<string, string> ExactZhLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Device"] = "设备",
        ["DeviceId"] = "设备Id",
        ["DeviceName"] = "设备名称",
        ["DeviceCode"] = "设备编码",
        ["Status"] = "状态",
        ["Location"] = "位置",
        ["CreateId"] = "创建人Id",
        ["Creator"] = "创建人",
        ["CreateDate"] = "创建时间",
        ["ModifyId"] = "修改人Id",
        ["Modifier"] = "修改人",
        ["ModifyDate"] = "修改时间",
        ["IsDeleted"] = "已删除",
        ["UserName"] = "用户名",
        ["UserTrueName"] = "真实姓名",
        ["PhoneNo"] = "手机号",
        ["RoleName"] = "角色名称",
        ["MenuName"] = "菜单名称",
        ["OrderNo"] = "排序号",
        ["Enable"] = "启用",
        ["Description"] = "描述",
        ["Remark"] = "备注",
        ["Code"] = "编码",
        ["Name"] = "名称",
        ["Title"] = "标题",
        ["Type"] = "类型",
        ["Level"] = "级别",
        ["Category"] = "分类",
        ["Source"] = "来源",
        ["Message"] = "消息",
    };

    static readonly Dictionary<string, string> WordZhLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = "Id",
        ["Name"] = "名称",
        ["Code"] = "编码",
        ["Status"] = "状态",
        ["Date"] = "日期",
        ["Time"] = "时间",
        ["Create"] = "创建",
        ["Modify"] = "修改",
        ["Update"] = "更新",
        ["Delete"] = "删除",
        ["Is"] = "是否",
        ["Deleted"] = "删除",
        ["User"] = "用户",
        ["Role"] = "角色",
        ["Menu"] = "菜单",
        ["Device"] = "设备",
        ["Order"] = "排序",
        ["No"] = "号",
        ["True"] = "真实",
        ["Phone"] = "电话",
        ["Enable"] = "启用",
        ["Type"] = "类型",
        ["Level"] = "级别",
        ["Desc"] = "描述",
        ["Description"] = "描述",
        ["Remark"] = "备注",
        ["Location"] = "位置",
        ["Creator"] = "人",
        ["Modifier"] = "人",
    };

    static void MergeLocaleFile(string path, string pageKey, Dictionary<string, string> entries)
    {
        var root = File.Exists(path)
            ? System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path)) as System.Text.Json.Nodes.JsonObject
              ?? new System.Text.Json.Nodes.JsonObject()
            : new System.Text.Json.Nodes.JsonObject();

        var generated = root["generated"] as System.Text.Json.Nodes.JsonObject
            ?? new System.Text.Json.Nodes.JsonObject();
        // 合并写入，避免旧版生成器整页覆盖时丢掉新增词条（如 deleteConfirm）
        var page = generated[pageKey] as System.Text.Json.Nodes.JsonObject
            ?? new System.Text.Json.Nodes.JsonObject();
        foreach (var (k, v) in entries)
            page[k] = v;
        generated[pageKey] = page;
        root["generated"] = generated;

        var json = root.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        FileHelper.WriteFile(path, json + Environment.NewLine);
    }

    static string SanitizeI18nKey(string tableName) =>
        string.Concat(tableName.Where(c => char.IsLetterOrDigit(c) || c == '_'));

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
            FolderName = string.IsNullOrWhiteSpace(req.FolderName)
                ? ProjectPath.ResolveVueFolder(null, req.Namespace)
                : req.FolderName,
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
        List<Sys_TableColumn> columns = [];

        if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            columns = await ReadMySqlColumnsAsync(tableName, cancellationToken);

        // information_schema 失败或参数未绑定时，回退到 EF 模型（Device 等已映射实体）
        if (columns.Count == 0)
            columns = ReadEfColumns(tableName);

        return columns;
    }

    /// <summary>解析真实物理表名：TableTrueName &gt; EF ToTable 名 &gt; 配置表名</summary>
    string ResolvePhysicalTableName(Sys_TableInfo tableInfo, string requestTableName)
    {
        if (!string.IsNullOrWhiteSpace(tableInfo.TableTrueName))
            return tableInfo.TableTrueName.Trim();

        var configName = string.IsNullOrWhiteSpace(tableInfo.TableName) ? requestTableName : tableInfo.TableName;
        return ResolvePhysicalTableNameByRequest(configName);
    }

    string ResolvePhysicalTableNameByRequest(string requestTableName)
    {
        var efName = _db.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .FirstOrDefault(n => n != null && n.Equals(requestTableName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(efName)) return efName;

        var byClr = _db.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name.Equals(requestTableName, StringComparison.OrdinalIgnoreCase));
        return byClr?.GetTableName() ?? requestTableName;
    }

    async Task<List<Sys_TableColumn>> ReadMySqlColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tableName) || !IsSafeSqlIdentifier(tableName))
            return [];

        // 候选物理表名：请求名 / 首字母大写（device→Device）
        var candidates = new[]
            {
                tableName,
                char.ToUpperInvariant(tableName[0]) + tableName[1..],
                tableName.ToLowerInvariant(),
            }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await _db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var conn = _db.Database.GetDbConnection();
            foreach (var name in candidates)
            {
                var cols = await QueryMySqlInformationSchemaAsync(conn, name, cancellationToken);
                if (cols.Count > 0) return cols;

                cols = await QueryMySqlShowColumnsAsync(conn, name, cancellationToken);
                if (cols.Count > 0) return cols;
            }
            return [];
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// 使用 DATABASE() + 安全字面量，避免 MySql @参数被当成用户变量导致永远查不到行。
    /// </summary>
    static async Task<List<Sys_TableColumn>> QueryMySqlInformationSchemaAsync(
        System.Data.Common.DbConnection conn, string tableName, CancellationToken cancellationToken)
    {
        // 表名已通过 IsSafeSqlIdentifier；用字面量避免 ADO 参数绑定差异
        var sql = $"""
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH,
                   IF(COLUMN_KEY = 'PRI', 1, 0),
                   IF(IS_NULLABLE = 'YES', 1, 0)
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND LOWER(TABLE_NAME) = LOWER('{tableName}')
            ORDER BY ORDINAL_POSITION
            """;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return await ReadColumnCommandAsync(cmd, cancellationToken);
    }

    static async Task<List<Sys_TableColumn>> QueryMySqlShowColumnsAsync(
        System.Data.Common.DbConnection conn, string tableName, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SHOW FULL COLUMNS FROM `{tableName}`";
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var result = new List<Sys_TableColumn>();
            var order = 100;
            while (await reader.ReadAsync(cancellationToken))
            {
                // Field, Type, Collation, Null, Key, Default, Extra, Privileges, Comment
                var field = reader.GetString(0);
                var type = reader.GetString(1);
                var dataType = type.Contains('(') ? type[..type.IndexOf('(')] : type;
                var nullable = reader.GetString(3);
                var key = reader.GetString(4);
                result.Add(new Sys_TableColumn
                {
                    ColumnName = field,
                    ColumnCNName = field,
                    ColumnType = dataType,
                    IsKey = key.Equals("PRI", StringComparison.OrdinalIgnoreCase),
                    IsNull = nullable.Equals("YES", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                    Editable = !key.Equals("PRI", StringComparison.OrdinalIgnoreCase),
                    Enable = 1,
                    IsDisplay = 1,
                    IsColumnData = 1,
                    OrderNo = order--,
                });
            }
            return result;
        }
        catch
        {
            return [];
        }
    }

    static async Task<List<Sys_TableColumn>> ReadColumnCommandAsync(
        System.Data.Common.DbCommand cmd, CancellationToken cancellationToken)
    {
        var result = new List<Sys_TableColumn>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var order = 100;
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new Sys_TableColumn
            {
                ColumnName = reader.GetString(0),
                ColumnCNName = reader.GetString(0),
                ColumnType = reader.GetString(1),
                Maxlength = ReadNullableMaxLength(reader, 2),
                IsKey = Convert.ToInt32(reader.GetValue(3)) == 1,
                IsNull = Convert.ToInt32(reader.GetValue(4)),
                Editable = Convert.ToInt32(reader.GetValue(3)) != 1,
                Enable = 1,
                IsDisplay = 1,
                IsColumnData = 1,
                OrderNo = order--,
            });
        }
        return result;
    }

    static int? ReadNullableMaxLength(System.Data.Common.DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        // MySQL longtext/mediumtext 的 CHARACTER_MAXIMUM_LENGTH 可能超过 Int32
        try
        {
            var value = Convert.ToInt64(reader.GetValue(ordinal));
            if (value <= 0) return null;
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }
        catch
        {
            return null;
        }
    }

    static bool IsSafeSqlIdentifier(string name) =>
        name.Length > 0 && name.Length <= 64 && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '$');

    string ResolveMySqlDatabaseName()
    {
        var connStr = _config["Database:ConnectionString"]
            ?? _config.GetConnectionString("Default")
            ?? "";
        return ExtractDatabaseName(connStr);
    }

    List<Sys_TableColumn> ReadEfColumns(string tableName)
    {
        var entityType = _db.Model.GetEntityTypes()
            .FirstOrDefault(e =>
                e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true
                || e.ClrType.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        if (entityType == null) return [];

        return entityType.GetProperties().Select((p, i) => new Sys_TableColumn
        {
            ColumnName = p.GetColumnName() ?? p.Name,
            ColumnCNName = p.Name,
            ColumnType = MapEfStoreType(p.GetColumnType() ?? p.ClrType.Name),
            IsKey = p.IsPrimaryKey(),
            IsNull = p.IsNullable ? 1 : 0,
            Editable = !p.IsPrimaryKey(),
            Enable = 1,
            IsDisplay = 1,
            IsColumnData = 1,
            OrderNo = 100 - i,
        }).ToList();
    }

    static string MapEfStoreType(string storeType)
    {
        var t = storeType.ToLowerInvariant();
        if (t.StartsWith("varchar") || t.StartsWith("nvarchar") || t.StartsWith("char")) return "nvarchar";
        if (t.Contains("int")) return t.Contains("big") ? "bigint" : "int";
        if (t.Contains("datetime") || t.Contains("timestamp")) return "datetime";
        if (t.Contains("decimal") || t.Contains("double") || t.Contains("float")) return "decimal";
        if (t is "bit" or "tinyint(1)") return "bool";
        return storeType;
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
