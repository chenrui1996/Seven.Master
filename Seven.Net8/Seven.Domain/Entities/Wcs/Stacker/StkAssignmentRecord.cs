using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>巷道轮转记录：最近一次分配。</summary>
public class StkAssignmentRecord : BaseEntity
{
    public int Id { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public DateTime LastAssignedAt { get; set; }
    public int AssignCount { get; set; }
}
