namespace Seven.Application.Wcs;

public record AllocationRequest(
    int WarehouseId,
    string PackId,
    decimal Height = 0,
    decimal Weight = 0,
    string? ContainerCode = null,
    string? ZoneCode = null,
    string? PreferredLayerCode = null,
    string? PreferredAisleCode = null);

public record AllocationStageResult(string Stage, string Code);

public record AllocationResult(
    bool Ok,
    string? LocationCode,
    string? LayerCode,
    string? AisleCode,
    string? Message,
    IReadOnlyList<AllocationStageResult> Stages);

public interface IWcsLocationSchema
{
    string PackId { get; }
    IReadOnlyList<string> HierarchyLevels { get; }
    bool IsValidCode(string code);
    string NormalizeCode(string code);
    string DescribeHierarchy();
}

public interface IWcsLocationAllocator
{
    string PackId { get; }
    Task<AllocationResult> AllocateInboundAsync(AllocationRequest request, CancellationToken ct = default);
}

public interface IWcsLocationAllocatorResolver
{
    IWcsLocationSchema GetSchema(string packId);
    IWcsLocationAllocator GetAllocator(string packId);
}
