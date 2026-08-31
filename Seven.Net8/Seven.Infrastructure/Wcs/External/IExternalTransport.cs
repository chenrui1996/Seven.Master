namespace Seven.Infrastructure.Wcs.External;

public record ExternalSendRequest(
    string PackId,
    string? BaseUrl,
    string Payload,
    string CorrelationId);

public record ExternalSendResult(bool Success, string? ErrorMessage = null);

/// <summary>外部 WCS 传输层（Http 或 Mq）。</summary>
public interface IExternalTransport
{
    ExternalTransportKind Kind { get; }

    Task<ExternalSendResult> SendAsync(ExternalSendRequest request, CancellationToken ct = default);
}

public enum ExternalTransportKind
{
    Http,
    Mq
}
