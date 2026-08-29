using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>库位主数据（全局权威，含交接位）</summary>
public class WmsLocation : BaseEntity
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int? ZoneId { get; set; }
    public int? LayerId { get; set; }
    public int? AisleId { get; set; }
    public string PackId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    /// <summary>冗余巷道编码（便于查询；权威也可走 AisleId）。</summary>
    public string? Aisle { get; set; }
    public string? Row { get; set; }
    public string? Column { get; set; }
    /// <summary>货架层号坐标（堆垛）；≠ Wms_Layer 主数据。</summary>
    public string? Layer { get; set; }
    public string? Depth { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsLocked { get; set; }
    public bool IsBooked { get; set; }
    public bool IsHandover { get; set; }
    public string? CurrentContainerCode { get; set; }
}
