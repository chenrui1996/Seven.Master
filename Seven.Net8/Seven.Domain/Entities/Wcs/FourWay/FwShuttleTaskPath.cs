using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向车任务路径点（节点序列）。</summary>
public class FwShuttleTaskPath : BaseEntity
{
    public int Id { get; set; }
    public Guid ShuttleTaskId { get; set; }
    public int Seq { get; set; }
    public string NodeCode { get; set; } = string.Empty;
    public string? EdgeId { get; set; }
}
