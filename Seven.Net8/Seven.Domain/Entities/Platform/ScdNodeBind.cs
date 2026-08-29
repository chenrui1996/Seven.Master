using Seven.Domain.Common;

namespace Seven.Domain.Entities.Platform;

/// <summary>2D SCADA 节点绑定（库位坐标映射，LocationCode 约定关联 Wms_Location.Code，无 FK）</summary>
public class ScdNodeBind : BaseEntity
{
    public int Id { get; set; }
    public int ViewId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string? Label { get; set; }
}
