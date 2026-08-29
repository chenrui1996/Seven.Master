using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛机出库取货任务，1:1 对应总线 Leg（对齐 LES RetrievalTask）。</summary>
public class StkRetrievalTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public StkRetrievalStatus Status { get; set; }
    /// <summary>组批号；同组按 WcsPri 顺序下发。</summary>
    public string WcsGroupNo { get; set; } = string.Empty;
    /// <summary>组内优先级，数值越小越先执行。</summary>
    public int WcsPri { get; set; }
}
