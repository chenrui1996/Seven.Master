using Seven.Application.Wcs;
using Seven.Application.Wms;

namespace Seven.Infrastructure.Wcs.Bus;

/// <summary>WMS 运输钩子 → 编排总线 CreateTransportOrder。</summary>
public sealed class BusTransportOrderRequest : ITransportOrderRequest
{
    private readonly IOrchestrationBus _bus;

    public BusTransportOrderRequest(IOrchestrationBus bus) => _bus = bus;

    public bool IsEnabled => true;

    public Task<Guid> RequestAsync(TransportOrderHookRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var to = string.IsNullOrWhiteSpace(request.ToLocationCode)
            ? request.FromLocationCode
            : request.ToLocationCode;
        var container = request.ContainerCode ?? string.Empty;
        return _bus.CreateTransportOrderAsync(
            new CreateTransportOrderRequest(
                container,
                request.FromLocationCode,
                to,
                request.RefType,
                request.RefId,
                request.WcsGroupNo,
                request.WcsPri),
            ct);
    }
}
