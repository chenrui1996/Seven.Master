using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IStockService
{
    Task<WmsStock> ReceiveAsync(ReceiveStockRequest request, CancellationToken ct = default);
    Task<WmsStock> ShipAsync(ShipStockRequest request, CancellationToken ct = default);
    Task<PageGridData<WmsStock>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
}
