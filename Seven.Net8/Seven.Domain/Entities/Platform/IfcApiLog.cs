using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Platform;

/// <summary>外部/内部接口调用日志。</summary>
public class IfcApiLog : BaseEntity
{
    public long Id { get; set; }
    public InterfaceLogDirection Direction { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? LegId { get; set; }
    public string? OrderNo { get; set; }
    public string? Path { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
