using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向目的地申请点（SUDR 语义入口）。</summary>
public class FwRequestPoint : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public FwRequestPointType PointType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? LayerCode { get; set; }
    public string? AisleCode { get; set; }
}
