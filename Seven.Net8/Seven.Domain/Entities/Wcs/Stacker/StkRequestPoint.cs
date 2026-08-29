using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛机目的地申请点（SUDR 语义入口）。</summary>
public class StkRequestPoint : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public StkRequestPointType PointType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? AisleCode { get; set; }
}
