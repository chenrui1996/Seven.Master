using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Bus;

/// <summary>编排总线运输单（跨包拆段）。</summary>
public class BusTransportOrder : BaseEntity
{
    public Guid Id { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromLocationCode { get; set; } = string.Empty;
    public string ToLocationCode { get; set; } = string.Empty;
    public BusOrderStatus Status { get; set; }
    public string? FailReason { get; set; }
    public string? RefType { get; set; }
    public string? RefId { get; set; }
    public ICollection<BusTransportLeg> Legs { get; set; } = new List<BusTransportLeg>();
}
