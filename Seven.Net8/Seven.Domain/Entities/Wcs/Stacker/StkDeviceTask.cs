using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛机设备段任务（目的地下发后跟踪；可多段 Seq）。</summary>
public class StkDeviceTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid? PutAwayTaskId { get; set; }
    public Guid? RetrievalTaskId { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromPointCode { get; set; } = string.Empty;
    public string DestinationPointCode { get; set; } = string.Empty;
    public string ExeStackCode { get; set; } = string.Empty;
    public int Seq { get; set; } = 1;
    public StkDeviceTaskStatus Status { get; set; }
}
