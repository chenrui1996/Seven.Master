using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IPickingService
{
    Task<PageGridData<WmsPickingTask>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<WmsPickingTask>> GenerateFromOutboundAsync(int outboundOrderId, CancellationToken ct = default);
    Task<WmsPickingTask> ConfirmPickAsync(ConfirmPickRequest request, CancellationToken ct = default);
    Task CancelAsync(int pickingTaskId, CancellationToken ct = default);
    Task<IReadOnlyList<WmsPickingTask>> ListPendingAsync(CancellationToken ct = default);
}

public record ConfirmPickRequest(
    int PickingTaskId,
    decimal? PickQty = null,
    string? ContainerCode = null,
    string? FromLocation = null,
    bool DispatchTransport = true);
