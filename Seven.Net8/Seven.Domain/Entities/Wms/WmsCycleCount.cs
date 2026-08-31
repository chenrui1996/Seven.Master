using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wms;

/// <summary>盘点单（计划头；行上合并账面/实盘，不另建 Record 表）</summary>
public class WmsCycleCount : BaseEntity
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public WmsOrderStatus Status { get; set; }
    public ICollection<WmsCycleCountLine> Lines { get; set; } = new List<WmsCycleCountLine>();
}
