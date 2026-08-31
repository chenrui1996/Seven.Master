using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Business;

/// <summary>
/// 仓内调拨单（业务扩展样板）。表：Biz_TransferOrder。
/// 库存变更只通过 IStockService，本表只存业务意图与状态。
/// </summary>
public class TransferOrder : BaseEntity
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    /// <summary>复用 WMS 单据状态机：Draft → Approved → Completed</summary>
    public WmsOrderStatus Status { get; set; }
    public string? Remark { get; set; }
    public ICollection<TransferOrderLine> Lines { get; set; } = new List<TransferOrderLine>();
}

/// <summary>调拨单行。表：Biz_TransferOrderLine。</summary>
public class TransferOrderLine : BaseEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int LineNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal CompletedQty { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? ContainerCode { get; set; }
    public TransferOrder? Order { get; set; }
}
