using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向车路网地图版本（冷配置）。</summary>
public class FwMapVersion : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
