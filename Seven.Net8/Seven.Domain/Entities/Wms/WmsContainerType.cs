using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>容器类型</summary>
public class WmsContainerType : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
