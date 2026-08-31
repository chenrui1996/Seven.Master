using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wms;

/// <summary>容器（托盘/料箱）</summary>
public class WmsContainer : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? ContainerTypeId { get; set; }
    public string? LocationCode { get; set; }
    public WmsContainerStatus Status { get; set; }
}
