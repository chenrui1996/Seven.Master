using Seven.Application.Wcs;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

public sealed class FourWayLocationSchema : IWcsLocationSchema
{
    public string PackId => WcsPackIds.FourWay;
    public IReadOnlyList<string> HierarchyLevels { get; } = ["Zone", "Layer", "Aisle", "Location"];

    public bool IsValidCode(string code) => PackCodeRules.HasValidPrefix(code, PackId);

    public string NormalizeCode(string code) => PackCodeRules.EnsurePrefix(code, PackId);

    public string DescribeHierarchy() => "Zone → Layer → Aisle → Location(=Node)";
}
