using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向层分配策略（对齐 RCS AssignmentPolicy.LayerCode）。</summary>
public class FwLayerPolicy : BaseEntity
{
    public int Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string LayerCode { get; set; } = string.Empty;
    public int MaxHeight { get; set; } = 9999;
    public decimal MaxWeight { get; set; } = 99999;
    public bool IsAvailable { get; set; } = true;
    /// <summary>分配权重，越大越优先（同权重再按轮转时间）。</summary>
    public int AllocationWeight { get; set; } = 1;
    public bool StartSign { get; set; }
    public int NextPolicyId { get; set; }
}
