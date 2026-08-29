using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IInboundOrderService
{
    Task<PageGridData<WmsInboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WmsInboundOrder> CreateAsync(CreateInboundOrderRequest request, CancellationToken ct = default);
    Task ApproveAsync(int orderId, CancellationToken ct = default);
    Task ReceiveAndBuildPalletAsync(
        int orderId,
        ReceiveAndBuildPalletRequest? request = null,
        CancellationToken ct = default);
}
