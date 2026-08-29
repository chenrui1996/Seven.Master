using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向层/巷轮转记录：最近一次分配。</summary>
public class FwAssignmentRecord : BaseEntity
{
    public int Id { get; set; }
    public FwAssignmentScopeType ScopeType { get; set; }
    public string ScopeCode { get; set; } = string.Empty;
    public DateTime LastAssignedAt { get; set; }
    public int AssignCount { get; set; }
}
