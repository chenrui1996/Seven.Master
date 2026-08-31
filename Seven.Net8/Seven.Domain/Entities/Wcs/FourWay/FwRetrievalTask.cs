using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向出库取货任务，1:1 对应总线 Leg（对齐堆垛 StkRetrievalTask）。</summary>
public class FwRetrievalTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public FwRetrievalStatus Status { get; set; }
    /// <summary>组批号；同组按 WcsPri 顺序下发。</summary>
    public string WcsGroupNo { get; set; } = string.Empty;
    /// <summary>组内优先级，数值越小越先执行。</summary>
    public int WcsPri { get; set; }
}
