using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向车路网边分组（最小骨架）。</summary>
public class FwRouteGroup : BaseEntity
{
    public int Id { get; set; }
    public int MapVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
