using Seven.Domain.Wcs;

namespace Seven.Domain.Wms;

/// <summary>货位/库区等 Code 的 Pack 前缀工具。</summary>
public static class PackCodeRules
{
    public static bool HasValidPrefix(string code, string packId)
    {
        var prefix = WcsPackIds.PrefixOf(packId);
        if (string.IsNullOrEmpty(prefix)) return false;
        return !string.IsNullOrWhiteSpace(code)
               && code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>若已有正确前缀则原样返回；否则加上前缀。</summary>
    public static string EnsurePrefix(string code, string packId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("编码不能为空", nameof(code));
        var trimmed = code.Trim();
        if (HasValidPrefix(trimmed, packId))
            return trimmed;
        var prefix = WcsPackIds.PrefixOf(packId);
        if (string.IsNullOrEmpty(prefix))
            throw new InvalidOperationException($"未知 PackId: {packId}");
        // 去掉错误的其它包前缀后再加
        foreach (var p in new[] { WcsPackIds.StackerPrefix, WcsPackIds.FourWayPrefix, WcsPackIds.BoxSortPrefix })
        {
            if (trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[p.Length..];
                break;
            }
        }
        return prefix + trimmed.TrimStart('.');
    }

    public static string? TryDetectPackId(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        if (code.StartsWith(WcsPackIds.StackerPrefix, StringComparison.OrdinalIgnoreCase)) return WcsPackIds.Stacker;
        if (code.StartsWith(WcsPackIds.FourWayPrefix, StringComparison.OrdinalIgnoreCase)) return WcsPackIds.FourWay;
        if (code.StartsWith(WcsPackIds.BoxSortPrefix, StringComparison.OrdinalIgnoreCase)) return WcsPackIds.BoxSort;
        return null;
    }
}
