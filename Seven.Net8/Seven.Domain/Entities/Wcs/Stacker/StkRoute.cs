using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛包内有向边（对齐 LES LesRoute 核心字段）。</summary>
public class StkRoute : BaseEntity
{
    public int Id { get; set; }
    /// <summary>地图分区；空串表示默认图。</summary>
    public string MapCode { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    /// <summary>连续同码可合并为一段 DeviceTask。</summary>
    public string ExeStackCode { get; set; } = string.Empty;
    public double Weight { get; set; } = 1;
    public int Capacity { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
}
