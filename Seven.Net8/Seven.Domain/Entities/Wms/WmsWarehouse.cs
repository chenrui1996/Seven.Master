using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>仓库主数据</summary>
public class WmsWarehouse : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
