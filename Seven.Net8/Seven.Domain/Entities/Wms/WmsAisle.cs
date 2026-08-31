using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>巷道主数据（堆垛必用；四向属某层）。</summary>
public class WmsAisle : BaseEntity
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int ZoneId { get; set; }
    public int? LayerId { get; set; }
    public string PackId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EpPointCode { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int AllocationWeight { get; set; } = 1;
    public int? MaxDepth { get; set; }
}
