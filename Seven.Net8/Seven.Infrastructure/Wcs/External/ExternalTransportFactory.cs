using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Wcs.External;

/// <summary>按配置 Transport 字段选择 Http / Mq 传输实例。</summary>
public sealed class ExternalTransportFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MqExternalTransport _mq;

    public ExternalTransportFactory(IHttpClientFactory httpClientFactory, MqExternalTransport mq)
    {
        _httpClientFactory = httpClientFactory;
        _mq = mq;
    }

    public IExternalTransport Create(ExternalWcsEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var kind = ParseTransport(options.Transport);
        return kind switch
        {
            ExternalTransportType.Http => new HttpExternalTransport(
                _httpClientFactory.CreateClient(ExternalWcsHttpClient.Name)),
            ExternalTransportType.Mq => _mq,
            _ => throw new InvalidOperationException($"Unsupported transport: {options.Transport}")
        };
    }

    public static ExternalTransportType ParseTransport(string? transport)
        => transport?.Trim().ToUpperInvariant() switch
        {
            "HTTP" => ExternalTransportType.Http,
            "MQ" => ExternalTransportType.Mq,
            _ => ExternalTransportType.Http
        };
}
