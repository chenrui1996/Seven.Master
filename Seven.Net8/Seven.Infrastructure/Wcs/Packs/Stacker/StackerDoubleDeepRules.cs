using Microsoft.EntityFrameworkCore;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>双深 / LockBin 规则（对齐 LES SelectLocation 核心）。</summary>
public static class StackerDoubleDeepRules
{
    public static int DepthValue(WmsLocation loc)
        => int.TryParse(loc.Depth, out var d) ? d : 1;

    /// <summary>孤二深：深位有货且同组浅位全空。</summary>
    public static bool IsOrphanDeep(
        WmsLocation candidate,
        IReadOnlyList<WmsLocation> groupLocs,
        IReadOnlyDictionary<string, StkLocationProfile> profilesByCode)
    {
        if (DepthValue(candidate) <= 1) return false;
        if (!profilesByCode.TryGetValue(candidate.Code, out var p) || string.IsNullOrWhiteSpace(p.BinGroupCode))
            return false;

        var sisters = groupLocs
            .Where(x => profilesByCode.TryGetValue(x.Code, out var sp)
                        && string.Equals(sp.BinGroupCode, p.BinGroupCode, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(x.Code, candidate.Code, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var shallow = sisters.Where(x => DepthValue(x) < DepthValue(candidate)).ToList();
        if (shallow.Count == 0) return false;
        return shallow.All(x => !x.IsOccupied && string.IsNullOrWhiteSpace(x.CurrentContainerCode));
    }

    /// <summary>入库选深位时：同组更浅位必须已占用（否则只许选浅位，避免造孤二深）。</summary>
    public static bool CanAllocateDeepForInbound(
        WmsLocation candidate,
        IReadOnlyList<WmsLocation> groupLocs,
        IReadOnlyDictionary<string, StkLocationProfile> profilesByCode)
    {
        if (DepthValue(candidate) <= 1) return true;
        if (!profilesByCode.TryGetValue(candidate.Code, out var p) || string.IsNullOrWhiteSpace(p.BinGroupCode))
            return true;

        var shallow = groupLocs
            .Where(x => profilesByCode.TryGetValue(x.Code, out var sp)
                        && string.Equals(sp.BinGroupCode, p.BinGroupCode, StringComparison.OrdinalIgnoreCase)
                        && DepthValue(x) < DepthValue(candidate))
            .ToList();
        if (shallow.Count == 0) return true;
        return shallow.Any(x => x.IsOccupied || x.IsBooked);
    }

    public static IReadOnlyList<WmsLocation> ShallowBlockers(
        WmsLocation deep,
        IReadOnlyList<WmsLocation> groupLocs,
        IReadOnlyDictionary<string, StkLocationProfile> profilesByCode)
    {
        if (DepthValue(deep) <= 1) return [];
        if (!profilesByCode.TryGetValue(deep.Code, out var p) || string.IsNullOrWhiteSpace(p.BinGroupCode))
            return [];

        return groupLocs
            .Where(x => profilesByCode.TryGetValue(x.Code, out var sp)
                        && string.Equals(sp.BinGroupCode, p.BinGroupCode, StringComparison.OrdinalIgnoreCase)
                        && DepthValue(x) < DepthValue(deep)
                        && (x.IsOccupied || !string.IsNullOrWhiteSpace(x.CurrentContainerCode)))
            .OrderByDescending(DepthValue)
            .ToList();
    }
}
