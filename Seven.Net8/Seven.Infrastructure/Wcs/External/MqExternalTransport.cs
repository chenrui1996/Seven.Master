using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Wcs.External;

/// <summary>MQ 传输骨架：Features.MessageQueue 关闭时抛 NotSupported；开启时为 no-op 发布占位。</summary>
public sealed class MqExternalTransport : IExternalTransport
{
    private readonly FeatureOptions _features;
    private readonly ILogger<MqExternalTransport>? _logger;

    public MqExternalTransport(IOptions<FeatureOptions> features, ILogger<MqExternalTransport>? logger = null)
    {
        _features = features.Value;
        _logger = logger;
    }

    public ExternalTransportKind Kind => ExternalTransportKind.Mq;

    public Task<ExternalSendResult> SendAsync(ExternalSendRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_features.MessageQueue)
            throw new NotSupportedException("MessageQueue feature is disabled; enable Features.MessageQueue for Mq transport.");

        _logger?.LogDebug(
            "MqExternalTransport no-op publish for {PackId}, correlation {CorrelationId}",
            request.PackId,
            request.CorrelationId);
        return Task.FromResult(new ExternalSendResult(true));
    }
}
