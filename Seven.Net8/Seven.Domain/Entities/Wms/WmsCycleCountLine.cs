using System.Text.Json.Serialization;
using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>盘点行：计划 + 实盘数量（Line+Record 合并）</summary>
public class WmsCycleCountLine : BaseEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int LineNo { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string? ContainerCode { get; set; }
    public decimal BookQty { get; set; }
    public decimal CountQty { get; set; }
    public decimal DiffQty { get; set; }
    public bool Counted { get; set; }

    [JsonIgnore]
    public WmsCycleCount? Order { get; set; }
}
