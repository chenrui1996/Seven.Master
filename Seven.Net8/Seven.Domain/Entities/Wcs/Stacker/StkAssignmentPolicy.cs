using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>巷道分配策略：高重上限、空位门槛与可用性。</summary>
public class StkAssignmentPolicy : BaseEntity
{
    public int Id { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int MaxHeight { get; set; }
    public int MaxWeight { get; set; }
    /// <summary>巷道内至少空闲货位数（0=不校验；对齐 LES 空位门槛时可设 ≥1）。</summary>
    public int MinEmptySlots { get; set; }
    /// <summary>分配权重，越大越优先（同权重再按轮转时间）。</summary>
    public int AllocationWeight { get; set; } = 1;
    public string DestinationPointCode { get; set; } = string.Empty;
}
