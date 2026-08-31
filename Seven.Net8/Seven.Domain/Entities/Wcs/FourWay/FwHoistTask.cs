using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>提升机业务任务：跨层意图（单 Bus Leg 内多阶段）。</summary>
public class FwHoistTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string HoistNo { get; set; } = string.Empty;
    public string SrcLayer { get; set; } = string.Empty;
    public string SrcAddress { get; set; } = string.Empty;
    public string DesLayer { get; set; } = string.Empty;
    public string DesAddress { get; set; } = string.Empty;
    /// <summary>业务起终点（Bus Leg From/To）。</summary>
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public FwHoistTaskStatus Status { get; set; }
    public FwHoistStage Stage { get; set; }
    public int WcsPri { get; set; }
}
