using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>巷道分配策略：高重上限与可用性。</summary>
public class StkAssignmentPolicy : BaseEntity
{
    public int Id { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int MaxHeight { get; set; }
    public int MaxWeight { get; set; }
    public string DestinationPointCode { get; set; } = string.Empty;
}
