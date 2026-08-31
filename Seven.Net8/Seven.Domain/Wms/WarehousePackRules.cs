using Seven.Domain.Wcs;

namespace Seven.Domain.Wms;

/// <summary>Warehouse.EnabledPackIds 解析与校验。</summary>
public static class WarehousePackRules
{
    public static IReadOnlyList<string> Parse(string? enabledPackIds)
    {
        if (string.IsNullOrWhiteSpace(enabledPackIds))
            return Array.Empty<string>();
        return enabledPackIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Where(WcsPackIds.IsKnown)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string Join(IEnumerable<string> packIds) =>
        string.Join(",", packIds
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(WcsPackIds.IsKnown)
            .Distinct(StringComparer.OrdinalIgnoreCase));

    public static string EnsureContains(string? enabledPackIds, string packId)
    {
        var list = Parse(enabledPackIds).ToList();
        var p = packId.Trim().ToLowerInvariant();
        if (!WcsPackIds.IsKnown(p))
            throw new WmsDomainException($"未知 PackId: {packId}");
        if (!list.Contains(p, StringComparer.OrdinalIgnoreCase))
            list.Add(p);
        if (list.Count == 0)
            throw new WmsDomainException("仓库必须至少启用一种 WCS 包（EnabledPackIds）");
        return Join(list);
    }

    public static void EnsureAtLeastOne(string? enabledPackIds)
    {
        if (Parse(enabledPackIds).Count == 0)
            throw new WmsDomainException("仓库必须至少启用一种 WCS 包（EnabledPackIds）");
    }

    public static bool Allows(string? enabledPackIds, string packId)
    {
        var p = (packId ?? string.Empty).Trim().ToLowerInvariant();
        return Parse(enabledPackIds).Contains(p, StringComparer.OrdinalIgnoreCase);
    }
}
