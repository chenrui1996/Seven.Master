using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seven.Business.WorkFlow;

/// <summary>步骤条件（对齐 Legrand FieldFilter）</summary>
public sealed class WorkFlowFieldFilter
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>运算符：= != &gt; &gt;= &lt; &lt;= in like or</summary>
    [JsonPropertyName("filterType")]
    public string? FilterType { get; set; }
}

/// <summary>基于业务行字典评估 Filters JSON（无条件视为匹配）</summary>
public static class WorkFlowFilterEvaluator
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static bool Matches(string? filtersJson, IReadOnlyDictionary<string, object?>? data)
    {
        if (string.IsNullOrWhiteSpace(filtersJson)) return true;
        List<WorkFlowFieldFilter>? filters;
        try
        {
            filters = JsonSerializer.Deserialize<List<WorkFlowFieldFilter>>(filtersJson, JsonOptions);
        }
        catch
        {
            return false;
        }

        if (filters == null || filters.Count == 0) return true;
        filters = filters.Where(f => !string.IsNullOrWhiteSpace(f.Field)).ToList();
        if (filters.Count == 0) return true;
        if (!filters.Any(f => !string.IsNullOrWhiteSpace(f.Value))) return true;
        if (data == null || data.Count == 0) return false;

        var andOk = true;
        var orHits = new List<bool>();
        var hasOr = false;

        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Value)) continue;
            var op = (filter.FilterType ?? "=").Trim().ToLowerInvariant();
            if (op is "or")
            {
                hasOr = true;
                orHits.Add(CompareEqual(GetValue(data, filter.Field), filter.Value.Trim()));
                continue;
            }

            if (!TryGetValue(data, filter.Field, out var actual))
            {
                andOk = false;
                continue;
            }

            andOk = andOk && Evaluate(actual, filter.Value.Trim(), op);
        }

        if (hasOr)
            andOk = andOk && orHits.Any(x => x);

        return andOk;
    }

    static bool Evaluate(object? actual, string expected, string op) =>
        op switch
        {
            "!=" or "<>" => !CompareEqual(actual, expected),
            ">" => CompareNumber(actual, expected) is { } c && c > 0,
            ">=" => CompareNumber(actual, expected) is { } c && c >= 0,
            "<" or "小于" => CompareNumber(actual, expected) is { } c && c < 0,
            "<=" => CompareNumber(actual, expected) is { } c && c <= 0,
            "in" => expected.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => CompareEqual(actual, v)),
            "like" or "contains" => (actual?.ToString() ?? string.Empty)
                .Contains(expected, StringComparison.OrdinalIgnoreCase),
            _ => CompareEqual(actual, expected),
        };

    static bool CompareEqual(object? actual, string expected)
    {
        if (actual == null) return string.IsNullOrEmpty(expected);
        if (actual is bool b)
        {
            if (bool.TryParse(expected, out var eb)) return b == eb;
            if (expected is "1" or "true" or "是") return b;
            if (expected is "0" or "false" or "否") return !b;
        }

        if (CompareNumber(actual, expected) is { } nc)
            return nc == 0;

        return string.Equals(Convert.ToString(actual, CultureInfo.InvariantCulture), expected, StringComparison.OrdinalIgnoreCase);
    }

    static int? CompareNumber(object? actual, string expected)
    {
        if (!TryToDecimal(actual, out var a) || !decimal.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out var e))
            return null;
        return a.CompareTo(e);
    }

    static bool TryToDecimal(object? actual, out decimal value)
    {
        value = 0;
        if (actual == null) return false;
        try
        {
            value = Convert.ToDecimal(actual, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return decimal.TryParse(Convert.ToString(actual, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }

    static bool TryGetValue(IReadOnlyDictionary<string, object?> data, string field, out object? value)
    {
        if (data.TryGetValue(field, out value)) return true;
        foreach (var kv in data)
        {
            if (string.Equals(kv.Key, field, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    static object? GetValue(IReadOnlyDictionary<string, object?> data, string field) =>
        TryGetValue(data, field, out var v) ? v : null;
}
