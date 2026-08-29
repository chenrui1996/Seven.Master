using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>提升机层口：层 ↔ 入/出 EP·AP（对齐 RCS HoistLayerPoint）。</summary>
public class FwHoistLayerPoint : BaseEntity
{
    public int Id { get; set; }
    public string LayerCode { get; set; } = string.Empty;
    public string HoistNo { get; set; } = string.Empty;
    public string? InboundEp { get; set; }
    public string? InboundAp { get; set; }
    public string? OutboundEp { get; set; }
    public string? OutboundAp { get; set; }
}
