using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>边占用（对齐 LesRouteFlow 简化：按 DeviceTask 占一席）。</summary>
public class StkRouteFlow : BaseEntity
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public Guid DeviceTaskId { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
}
