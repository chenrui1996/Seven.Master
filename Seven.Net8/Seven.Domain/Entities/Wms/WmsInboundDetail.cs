using System.Text.Json.Serialization;
using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wms;

/// <summary>入库组盘明细（对齐 LES OrderDetail / StorageTask 意图源）。</summary>
public class WmsInboundDetail : BaseEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int LineId { get; set; }
    public int DetailNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string ReceiveLocationCode { get; set; } = string.Empty;
    public string? TargetLocationCode { get; set; }
    public string? AssignedAisle { get; set; }
    public string? AssignedLayer { get; set; }
    public string PackId { get; set; } = string.Empty;
    public WmsInboundDetailStatus Status { get; set; }
    public Guid? TransportOrderId { get; set; }

    [JsonIgnore]
    public WmsInboundOrder? Order { get; set; }

    [JsonIgnore]
    public WmsInboundOrderLine? Line { get; set; }
}
