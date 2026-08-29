using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向入库上架任务，1:1 对应总线 Leg。</summary>
public class FwPutAwayTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public FwPutAwayStatus Status { get; set; }
    public string? AssignedLayer { get; set; }
    public string? AssignedAisle { get; set; }
    public string? AssignedLocationCode { get; set; }
}
