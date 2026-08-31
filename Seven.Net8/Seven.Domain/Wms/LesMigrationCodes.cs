using Seven.Domain.Wcs;

namespace Seven.Domain.Wms;

/// <summary>LES 裸码 → Seven 带类型段前缀码（对齐 design/wms/02 §7）。</summary>
public static class LesMigrationCodes
{
    /// <summary>LES ZoneType：DenseWarehouse=30, MiniLoad=31。</summary>
    public const byte LesZoneDense = 30;
    public const byte LesZoneMiniLoad = 31;
    public const byte LesZoneAsrs = 10;

    public static string MapPackIdFromZoneType(byte zoneType, string denseOrMiniLoadPackId = WcsPackIds.Stacker) =>
        zoneType is LesZoneDense or LesZoneMiniLoad
            ? (string.IsNullOrWhiteSpace(denseOrMiniLoadPackId) ? WcsPackIds.Stacker : denseOrMiniLoadPackId.Trim())
            : WcsPackIds.Stacker;

    /// <summary>汇总仓库 EnabledPackIds（逗号分隔、去重保序）。</summary>
    public static string BuildEnabledPackIds(IEnumerable<byte> zoneTypes, string denseOrMiniLoadPackId = WcsPackIds.Stacker)
    {
        var packs = new List<string>();
        foreach (var t in zoneTypes)
        {
            var p = MapPackIdFromZoneType(t, denseOrMiniLoadPackId);
            if (!packs.Contains(p, StringComparer.OrdinalIgnoreCase))
                packs.Add(p);
        }

        if (packs.Count == 0)
            packs.Add(WcsPackIds.Stacker);
        return string.Join(',', packs);
    }

    public static string ToZoneCode(string lesCode, string packId) =>
        WithKind(lesCode, packId, "Z-");

    public static string ToAisleCode(string lesCode, string packId) =>
        WithKind(lesCode, packId, "A-");

    /// <summary>堆垛货位：Stk.B-{裸码}。</summary>
    public static string ToBinLocationCode(string lesCode, string packId) =>
        WithKind(lesCode, packId, "B-");

    /// <summary>四向节点：Fw.N-{裸码}。</summary>
    public static string ToNodeLocationCode(string lesCode, string packId)
    {
        var raw = StripKnownPrefixes(lesCode);
        if (raw.StartsWith("N-", StringComparison.OrdinalIgnoreCase))
            raw = raw[2..];
        else if (raw.Length > 1 && (raw[0] is 'N' or 'n') && char.IsDigit(raw[1]))
            raw = raw[1..];
        return WcsPackIds.PrefixOf(packId) + "N-" + raw;
    }

    /// <summary>按 Pack 选择货位段：fourway→N-，否则 B-。</summary>
    public static string ToLocationCode(string lesCode, string packId) =>
        string.Equals(packId, WcsPackIds.FourWay, StringComparison.OrdinalIgnoreCase)
            ? ToNodeLocationCode(lesCode, packId)
            : ToBinLocationCode(lesCode, packId);

    /// <summary>层主数据：L2 / 2 → Fw.L02。</summary>
    public static string ToLayerCode(string lesCode, string packId)
    {
        var raw = StripKnownPrefixes(lesCode);
        if (raw.StartsWith("L-", StringComparison.OrdinalIgnoreCase))
            raw = raw[2..];
        else if (raw.StartsWith('L') || raw.StartsWith('l'))
            raw = raw[1..];

        if (int.TryParse(raw, out var n) && n >= 0 && n < 1000)
            return WcsPackIds.PrefixOf(packId) + $"L{n:D2}";

        return WithKind(lesCode, packId, "L-");
    }

    private static string WithKind(string lesCode, string packId, string kindPrefix)
    {
        if (string.IsNullOrWhiteSpace(lesCode))
            throw new ArgumentException("编码不能为空", nameof(lesCode));
        var prefix = WcsPackIds.PrefixOf(packId);
        if (string.IsNullOrEmpty(prefix))
            throw new InvalidOperationException($"未知 PackId: {packId}");

        var raw = StripKnownPrefixes(lesCode);
        // 去掉已有类型段（Z-/A-/B-/N-/L-）
        foreach (var k in new[] { "Z-", "A-", "B-", "N-", "L-" })
        {
            if (raw.StartsWith(k, StringComparison.OrdinalIgnoreCase))
            {
                raw = raw[k.Length..];
                break;
            }
        }

        return prefix + kindPrefix + raw;
    }

    private static string StripKnownPrefixes(string code)
    {
        var trimmed = code.Trim();
        foreach (var p in new[] { WcsPackIds.StackerPrefix, WcsPackIds.FourWayPrefix, WcsPackIds.BoxSortPrefix })
        {
            if (trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                return trimmed[p.Length..];
        }

        return trimmed;
    }
}
