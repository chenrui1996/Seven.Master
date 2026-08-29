using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>楼层主数据（四向等使用；堆垛可不建行）。</summary>
public class WmsLayer : BaseEntity
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int ZoneId { get; set; }
    public string PackId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int AllocationWeight { get; set; } = 1;
    public int? MaxShuttleCount { get; set; }
}
