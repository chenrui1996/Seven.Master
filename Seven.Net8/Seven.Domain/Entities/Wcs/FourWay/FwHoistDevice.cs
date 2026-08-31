using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向提升机台账（对齐 RCS HoistDevice）。</summary>
public class FwHoistDevice : BaseEntity
{
    public int Id { get; set; }
    public string HoistNo { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? CurrentLayer { get; set; }
    public string? CurrentLocation { get; set; }
    public bool IsAvailable { get; set; } = true;
}
