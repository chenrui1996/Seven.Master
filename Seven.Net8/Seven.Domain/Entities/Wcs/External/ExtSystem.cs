using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.External;

/// <summary>外部 WCS 系统实例配置（PackId + 传输 + Codec）。</summary>
public class ExtSystem : BaseEntity
{
    public Guid Id { get; set; }
    public string PackId { get; set; } = string.Empty;
    public ExternalTransportType Transport { get; set; }
    public string Codec { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool Enabled { get; set; } = true;
}
