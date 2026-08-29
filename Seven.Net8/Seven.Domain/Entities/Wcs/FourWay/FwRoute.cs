using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向车有向边：权重 + 容量，禁止与 Stk_ 路网混用。</summary>
public class FwRoute : BaseEntity
{
    public int Id { get; set; }
    public int MapVersionId { get; set; }
    public int? RouteGroupId { get; set; }
    public int FromNodeId { get; set; }
    public int ToNodeId { get; set; }
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public double Weight { get; set; } = 1;
    public int Capacity { get; set; } = 1;
}
