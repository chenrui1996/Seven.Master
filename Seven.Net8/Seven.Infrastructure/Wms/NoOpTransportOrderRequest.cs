using Seven.Application.Wms;

namespace Seven.Infrastructure.Wms;

/// <summary>Features.OrchestrationBus 关闭时的空实现。</summary>
public sealed class NoOpTransportOrderRequest : ITransportOrderRequest
{
    public bool IsEnabled => false;

    public Task RequestAsync(TransportOrderHookRequest request, CancellationToken ct = default)
        => Task.CompletedTask;
}
