using Microsoft.Extensions.Logging;
using Seven.Application.Wms;

namespace Seven.Infrastructure.Wcs.Bus;

/// <summary>V1 占位：仅记录日志。后续 Task 可回写入库单/库存。</summary>
public sealed class NoOpWmsTransportCompletionHandler : IWmsTransportCompletionHandler
{
    private readonly ILogger<NoOpWmsTransportCompletionHandler> _logger;

    public NoOpWmsTransportCompletionHandler(ILogger<NoOpWmsTransportCompletionHandler> logger)
        => _logger = logger;

    public Task OnTransportCompletedAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("TransportOrder {OrderId} completed; WMS ledger hook is NoOp", orderId);
        return Task.CompletedTask;
    }
}
