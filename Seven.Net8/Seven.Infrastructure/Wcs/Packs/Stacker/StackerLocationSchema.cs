using Seven.Application.Wcs;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

public sealed class StackerLocationSchema : IWcsLocationSchema
{
    public string PackId => WcsPackIds.Stacker;
    public IReadOnlyList<string> HierarchyLevels { get; } = ["Zone", "Aisle", "Location"];

    public bool IsValidCode(string code) => PackCodeRules.HasValidPrefix(code, PackId);

    public string NormalizeCode(string code) => PackCodeRules.EnsurePrefix(code, PackId);

    public string DescribeHierarchy() => "Zone → Aisle → Location(Row,Col,Layer,Depth)";
}
