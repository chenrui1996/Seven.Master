using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向巷分配策略（对齐 RCS AssignmentPolicy.AisleCode）。</summary>
public class FwAislePolicy : BaseEntity
{
    public int Id { get; set; }
    public string LayerCode { get; set; } = string.Empty;
    public string AisleCode { get; set; } = string.Empty;
    /// <summary>巷道内至少空闲货位数（0=不校验）。</summary>
    public int MinEmptySlots { get; set; }
    public int MaxShuttleCount { get; set; }
    public string DestinationPointCode { get; set; } = string.Empty;
    /// <summary>分配权重，越大越优先（同权重再按轮转时间）。</summary>
    public int AllocationWeight { get; set; } = 1;
    public bool IsAvailable { get; set; } = true;
    public int MaxHeight { get; set; } = 9999;
    public decimal MaxWeight { get; set; } = 99999;
}
