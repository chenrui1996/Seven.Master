using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>入库单行</summary>
public class WmsInboundOrderLine : BaseEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int LineNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal CompletedQty { get; set; }
    public string? ContainerCode { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public WmsInboundOrder? Order { get; set; }
    public ICollection<WmsInboundDetail> Details { get; set; } = new List<WmsInboundDetail>();
}
