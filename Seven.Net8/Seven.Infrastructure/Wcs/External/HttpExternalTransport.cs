using System.Net.Http.Headers;
using System.Text;

namespace Seven.Infrastructure.Wcs.External;

/// <summary>HTTP POST 下发外部 WCS 报文。</summary>
public sealed class HttpExternalTransport : IExternalTransport
{
    private readonly HttpClient _httpClient;

    public HttpExternalTransport(HttpClient httpClient)
        => _httpClient = httpClient;

    public ExternalTransportKind Kind => ExternalTransportKind.Http;

    public async Task<ExternalSendResult> SendAsync(ExternalSendRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.BaseUrl))
            return new ExternalSendResult(false, "BaseUrl is required for Http transport");

        var url = request.BaseUrl.TrimEnd('/');
        using var content = new StringContent(request.Payload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        try
        {
            using var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? new ExternalSendResult(true)
                : new ExternalSendResult(false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return new ExternalSendResult(false, ex.Message);
        }
    }
}
