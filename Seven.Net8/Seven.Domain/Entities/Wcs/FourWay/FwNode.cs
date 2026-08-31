using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向车路网节点；LocationCode 关联 Wms_Location.Code。</summary>
public class FwNode : BaseEntity
{
    public int Id { get; set; }
    public int MapVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? LocationCode { get; set; }
}
