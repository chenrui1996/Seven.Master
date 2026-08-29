using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛机设备段任务（目的地下发后跟踪）。</summary>
public class StkDeviceTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PutAwayTaskId { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string DestinationPointCode { get; set; } = string.Empty;
    public StkDeviceTaskStatus Status { get; set; }
}
