using Seven.Application.Wcs;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>堆垛入库分配：巷道策略 → 巷内空闲货位。</summary>
public sealed class StackerInboundAllocator : IWcsLocationAllocator
{
    private readonly StackerAisleAllocator _aisle;
    private readonly StackerLocationAllocator _location;

    public StackerInboundAllocator(StackerAisleAllocator aisle, StackerLocationAllocator location)
    {
        _aisle = aisle;
        _location = location;
    }

    public string PackId => WcsPackIds.Stacker;

    public async Task<AllocationResult> AllocateInboundAsync(AllocationRequest request, CancellationToken ct = default)
    {
        var stages = new List<AllocationStageResult>();
        string aisleCode;
        if (!string.IsNullOrWhiteSpace(request.PreferredAisleCode))
        {
            aisleCode = request.PreferredAisleCode.Trim();
            stages.Add(new AllocationStageResult("Aisle", aisleCode));
        }
        else
        {
            var aisle = await _aisle.SelectAisleAsync((int)request.Height, (int)request.Weight, ct);
            if (aisle == null)
                return new AllocationResult(false, null, null, null, "无可用巷道策略", stages);
            aisleCode = aisle.AisleCode;
            stages.Add(new AllocationStageResult("Aisle", aisleCode));
        }

        var locCode = await _location.SelectLocationAsync(aisleCode, request.WarehouseId, book: true, ct);
        if (string.IsNullOrEmpty(locCode))
            return new AllocationResult(false, null, null, aisleCode, "巷道内无空闲货位", stages);

        stages.Add(new AllocationStageResult("Location", locCode));
        return new AllocationResult(true, locCode, null, aisleCode, null, stages);
    }
}
