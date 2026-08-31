using Seven.Domain.Common;
using Seven.Domain.Entities.Business;

namespace Seven.Application.Business;

/// <summary>仓内调拨（业务扩展样板契约）。实现在 Seven.Business。</summary>
public interface ITransferOrderService
{
    Task<PageGridData<TransferOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<TransferOrder?> GetAsync(int orderId, CancellationToken ct = default);
    Task<TransferOrder> CreateAsync(CreateTransferOrderRequest request, CancellationToken ct = default);
    /// <summary>草稿 → 已审：对源库位 Book 预约。</summary>
    Task ApproveAsync(int orderId, CancellationToken ct = default);
    /// <summary>已审 → 完成：ConfirmPick(源) + Receive(目标)。</summary>
    Task CompleteAsync(int orderId, CancellationToken ct = default);
}

public record TransferLineInput(
    int LineNo,
    string MaterialCode,
    decimal Qty,
    string? FromLocation = null,
    string? ToLocation = null,
    string? ContainerCode = null);

public record CreateTransferOrderRequest(
    string OrderNo,
    IReadOnlyList<TransferLineInput>? Lines = null,
    string? Remark = null);
