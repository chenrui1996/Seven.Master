using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wms;

/// <summary>入库单（仅此一套；来源见 OrderType）</summary>
public class WmsInboundOrder : BaseEntity
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public WmsOrderType OrderType { get; set; }
    public WmsOrderStatus Status { get; set; }
    public ICollection<WmsInboundOrderLine> Lines { get; set; } = new List<WmsInboundOrderLine>();
}
