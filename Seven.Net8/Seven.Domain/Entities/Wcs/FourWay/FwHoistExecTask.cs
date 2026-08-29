using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>提升机执行任务：排队、下发、完成/失败（同口联锁）。</summary>
public class FwHoistExecTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid HoistTaskId { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string HoistNo { get; set; } = string.Empty;
    public string SrcLayer { get; set; } = string.Empty;
    public string SrcAddress { get; set; } = string.Empty;
    public string DesLayer { get; set; } = string.Empty;
    public string DesAddress { get; set; } = string.Empty;
    public FwHoistExecStatus Status { get; set; }
    public int WcsPri { get; set; }
}
