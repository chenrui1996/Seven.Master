using Seven.Domain.Common;
using Seven.Domain.Entities.Bus;

namespace Seven.Application.Wcs;

public interface IBusTransportOrderQuery
{
    Task<PageGridData<BusTransportOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
}
