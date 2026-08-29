using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>库存流水</summary>
public class WmsStockLedger : BaseEntity
{
    public int Id { get; set; }
    public int? StockId { get; set; }
    public WmsStock? Stock { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string? ContainerCode { get; set; }
    public decimal DeltaQty { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RefType { get; set; }
    public string? RefId { get; set; }
}
