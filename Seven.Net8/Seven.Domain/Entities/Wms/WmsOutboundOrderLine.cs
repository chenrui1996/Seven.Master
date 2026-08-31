using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>出库单行</summary>
public class WmsOutboundOrderLine : BaseEntity
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
    /// <summary>组内优先级；0 表示审核时按 LineNo 赋值。</summary>
    public int WcsPri { get; set; }
    public WmsOutboundOrder? Order { get; set; }
    public ICollection<WmsPickingTask> PickingTasks { get; set; } = new List<WmsPickingTask>();
}
