using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface ICycleCountService
{
    Task<PageGridData<WmsCycleCount>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WmsCycleCount> CreatePlanAsync(CreateCycleCountRequest request, CancellationToken ct = default);
    Task RecordCountAsync(int orderId, int lineNo, decimal countQty, CancellationToken ct = default);
    Task ConfirmAdjustAsync(int orderId, CancellationToken ct = default);
    Task<WmsCycleCount?> GetAsync(int orderId, CancellationToken ct = default);
}
