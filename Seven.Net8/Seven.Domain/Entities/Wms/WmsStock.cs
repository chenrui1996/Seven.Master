using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>库位级库存账本</summary>
public class WmsStock : BaseEntity
{
    public int Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string? ContainerCode { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal AvailableQty { get; set; }
    public string? Lot { get; set; }
}
