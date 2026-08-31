using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wms;

/// <summary>出库拣选任务（出库单第三层：Order → Line → PickingTask）。</summary>
public class WmsPickingTask : BaseEntity
{
    public int Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public int OutboundOrderId { get; set; }
    public int LineId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal BookQty { get; set; }
    public decimal PickQty { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? ContainerCode { get; set; }
    public int WcsPri { get; set; }
    public WmsPickingTaskStatus Status { get; set; }
    public Guid? TransportOrderId { get; set; }
    public WmsOutboundOrder? Order { get; set; }
    public WmsOutboundOrderLine? Line { get; set; }
}
