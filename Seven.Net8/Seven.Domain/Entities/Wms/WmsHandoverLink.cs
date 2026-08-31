using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>跨包交接链：FromPack → Location → ToPack。</summary>
public class WmsHandoverLink : BaseEntity
{
    public int Id { get; set; }
    public string FromPackId { get; set; } = string.Empty;
    public string ToPackId { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
}
