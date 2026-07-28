using System.Globalization;
using System.Reflection;
using MiniExcelLibs;

namespace Seven.Infrastructure.Excel;

/// <summary>生成 CRUD 页通用 Excel 导入/导出</summary>
public static class CrudExcelHelper
{
    static readonly HashSet<string> SkipAlways = new(StringComparer.OrdinalIgnoreCase)
    {
        "IsDeleted",
    };

    static readonly HashSet<string> SkipImport = new(StringComparer.OrdinalIgnoreCase)
    {
        "IsDeleted",
        "CreateId",
        "Creator",
        "CreateDate",
        "ModifyId",
        "Modifier",
        "ModifyDate",
    };

    const int ExportMaxRows = 10000;

    public static int MaxExportRows => ExportMaxRows;

    /// <summary>导出实体列表为 xlsx（表头为属性名）</summary>
    public static byte[] Export<T>(IEnumerable<T> rows, string? keyPropertyName = null)
    {
        var props = GetExportProperties(typeof(T));
        var list = rows.Select(row =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var p in props)
                dict[p.Name] = p.GetValue(row);
            return dict;
        }).ToList();

        using var stream = new MemoryStream();
        stream.SaveAs(list);
        return stream.ToArray();
    }

    /// <summary>仅表头模板（导入用，排除主键与审计字段）</summary>
    public static byte[] BuildTemplate<T>(string keyPropertyName)
    {
        var props = GetImportProperties(typeof(T), keyPropertyName);
        var headers = props.Select(p => p.Name).ToArray();
        var empty = new Dictionary<string, object?>();
        foreach (var h in headers)
            empty[h] = null;

        using var stream = new MemoryStream();
        stream.SaveAs(new[] { empty });
        return stream.ToArray();
    }

    /// <summary>从 xlsx 解析实体列表（按表头属性名映射）</summary>
    public static List<T> Import<T>(Stream stream, string keyPropertyName) where T : new()
    {
        var props = GetImportProperties(typeof(T), keyPropertyName);
        var propMap = props.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        var result = new List<T>();

        foreach (var row in MiniExcel.Query(stream, useHeaderRow: true))
        {
            if (row is not IDictionary<string, object> dict) continue;
            if (dict.Values.All(v => v == null || string.IsNullOrWhiteSpace(Convert.ToString(v))))
                continue;

            var entity = new T();
            foreach (var (header, raw) in dict)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;
                if (!propMap.TryGetValue(header.Trim(), out var prop)) continue;
                if (raw == null || (raw is string s && string.IsNullOrWhiteSpace(s))) continue;
                try
                {
                    prop.SetValue(entity, ConvertTo(raw, prop.PropertyType));
                }
                catch
                {
                    // 单字段转换失败则跳过该字段
                }
            }
            result.Add(entity);
        }

        return result;
    }

    static List<PropertyInfo> GetExportProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !SkipAlways.Contains(p.Name))
            .ToList();

    static List<PropertyInfo> GetImportProperties(Type type, string keyPropertyName) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite
                        && !SkipImport.Contains(p.Name)
                        && !p.Name.Equals(keyPropertyName, StringComparison.OrdinalIgnoreCase))
            .ToList();

    static object? ConvertTo(object raw, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (raw is string str && underlying != typeof(string))
        {
            if (underlying == typeof(DateTime) && DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (underlying.IsEnum)
                return Enum.Parse(underlying, str, true);
        }

        if (underlying.IsEnum)
            return Enum.ToObject(underlying, Convert.ChangeType(raw, Enum.GetUnderlyingType(underlying), CultureInfo.InvariantCulture)!);

        return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
    }
}
