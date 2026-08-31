using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IInboundOrderService
{
    Task<PageGridData<WmsInboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WmsInboundOrder?> GetAsync(int orderId, CancellationToken ct = default);
    Task<WmsInboundOrder> CreateAsync(CreateInboundOrderRequest request, CancellationToken ct = default);
    Task ApproveAsync(int orderId, CancellationToken ct = default);

    /// <summary>整单剩余量组盘（兼容旧 API）；缺 ToLocation 时按包 Allocator 推荐。</summary>
    Task ReceiveAndBuildPalletAsync(
        int orderId,
        ReceiveAndBuildPalletRequest? request = null,
        CancellationToken ct = default);

    /// <summary>Detail 级组盘：写 InboundDetail、收货入账、可选建运输。</summary>
    Task<WmsInboundDetail> BuildPalletAsync(
        int orderId,
        BuildPalletRequest request,
        CancellationToken ct = default);
}
