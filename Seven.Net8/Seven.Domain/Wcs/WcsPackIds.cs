namespace Seven.Domain.Wcs;

/// <summary>WCS 包标识与编码前缀（设计：wms/02）。</summary>
public static class WcsPackIds
{
    public const string Stacker = "stacker";
    public const string FourWay = "fourway";
    public const string BoxSort = "boxsort";

    public const string StackerPrefix = "Stk.";
    public const string FourWayPrefix = "Fw.";
    public const string BoxSortPrefix = "Bs.";

    public static string PrefixOf(string packId) =>
        (packId ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            Stacker => StackerPrefix,
            FourWay => FourWayPrefix,
            BoxSort => BoxSortPrefix,
            _ => string.Empty
        };

    public static bool IsKnown(string packId)
    {
        var p = (packId ?? string.Empty).Trim().ToLowerInvariant();
        return p is Stacker or FourWay or BoxSort || p.StartsWith("external:", StringComparison.Ordinal);
    }
}
