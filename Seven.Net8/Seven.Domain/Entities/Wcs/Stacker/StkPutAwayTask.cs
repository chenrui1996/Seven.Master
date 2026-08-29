using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛机上架任务，1:1 对应总线 Leg。</summary>
public class StkPutAwayTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public StkPutAwayStatus Status { get; set; }
    public string? AssignedAisle { get; set; }
    public string? AssignedLocationCode { get; set; }
}
