using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>库区主数据</summary>
public class WmsZone : BaseEntity
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string PackId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
