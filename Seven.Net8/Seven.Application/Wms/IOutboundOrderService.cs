using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IOutboundOrderService
{
    Task<PageGridData<WmsOutboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WmsOutboundOrder> CreateAsync(CreateOutboundOrderRequest request, CancellationToken ct = default);
    Task ApproveAsync(int orderId, CancellationToken ct = default);
    Task AllocateAndReserveAsync(int orderId, CancellationToken ct = default);
    Task ShipAsync(int orderId, CancellationToken ct = default);
}
