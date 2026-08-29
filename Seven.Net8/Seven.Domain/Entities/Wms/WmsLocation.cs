using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>库位主数据（全局权威，含交接位）</summary>
public class WmsLocation : BaseEntity
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int? ZoneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Row { get; set; }
    public string? Column { get; set; }
    public string? Layer { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsLocked { get; set; }
    public bool IsHandover { get; set; }
    public string? CurrentContainerCode { get; set; }
}
