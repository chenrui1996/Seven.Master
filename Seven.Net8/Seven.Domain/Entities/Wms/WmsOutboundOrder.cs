using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wms;

/// <summary>出库单（仅此一套；来源见 OrderType）</summary>
public class WmsOutboundOrder : BaseEntity
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public WmsOrderType OrderType { get; set; }
    public WmsOrderStatus Status { get; set; }
    /// <summary>WCS 组批号；空则审核时用 OrderNo。</summary>
    public string? WcsGroupNo { get; set; }
    public ICollection<WmsOutboundOrderLine> Lines { get; set; } = new List<WmsOutboundOrderLine>();
}
