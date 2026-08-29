using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.External;

/// <summary>外部 WCS 报文日志。</summary>
public class ExtMessageLog : BaseEntity
{
    public long Id { get; set; }
    public string SystemPackId { get; set; } = string.Empty;
    public InterfaceLogDirection Direction { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public bool Success { get; set; }
}
