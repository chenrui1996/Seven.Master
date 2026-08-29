using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Bus;

/// <summary>运输单在某一 WCS 包上的执行段。</summary>
public class BusTransportLeg : BaseEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string PackId { get; set; } = string.Empty;
    public int Seq { get; set; }
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public string ContainerCode { get; set; } = string.Empty;
    public string? HandoverIn { get; set; }
    public string? HandoverOut { get; set; }
    public BusLegStatus Status { get; set; }
    public string? Message { get; set; }
    public BusTransportOrder Order { get; set; } = null!;
}
