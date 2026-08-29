using Seven.Domain.Common;
using Seven.Domain.Entities.Bus;

namespace Seven.Application.Wcs;

public interface IBusTransportOrderQuery
{
    Task<PageGridData<BusTransportOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<BusTransportOrder?> GetWithLegsAsync(Guid orderId, CancellationToken ct = default);
    Task<PageGridData<BusTransportLeg>> GetLegsPageAsync(PageDataOptions options, CancellationToken ct = default);
}
