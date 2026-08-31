using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IStockService
{
    Task<WmsStock> ReceiveAsync(ReceiveStockRequest request, CancellationToken ct = default);
    Task<WmsStock> ShipAsync(ShipStockRequest request, CancellationToken ct = default);
    /// <summary>预约/锁定可用量（AvailableQty -= qty，Qty 不变）。</summary>
    Task<WmsStock> BookAsync(BookStockRequest request, CancellationToken ct = default);
    /// <summary>拣货确认扣账：在已预约前提下减少 Qty（AvailableQty 已扣则不再二次扣可用）。</summary>
    Task<WmsStock> ConfirmPickAsync(ConfirmPickStockRequest request, CancellationToken ct = default);
    Task ReleaseBookAsync(BookStockRequest request, CancellationToken ct = default);
    Task<PageGridData<WmsStock>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
}
