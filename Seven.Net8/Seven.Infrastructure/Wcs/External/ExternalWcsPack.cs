using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.External;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.External;

/// <summary>外部 WCS 包：Codec 编码 + Transport 下发 + 回调归一总线事件。</summary>
public sealed class ExternalWcsPack : IWcsPack
{
    private readonly ExternalWcsEntryOptions _options;
    private readonly IExternalTransport _transport;
    private readonly IVendorCodec _codec;
    private readonly SevenDbContext _db;
    private readonly IOrchestrationBus? _bus;

    public ExternalWcsPack(
        ExternalWcsEntryOptions options,
        IExternalTransport transport,
        IVendorCodec codec,
        SevenDbContext db,
        IOrchestrationBus? bus = null)
    {
        _options = options;
        _transport = transport;
        _codec = codec;
        _db = db;
        _bus = bus;
    }

    public string PackId => _options.PackId;

    public Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default)
        => Task.FromResult(_options.Enabled);

    public async Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(leg);
        if (!_options.Enabled)
            return new AcceptLegResult(false, "External WCS pack is disabled");

        if (!string.Equals(leg.PackId, PackId, StringComparison.OrdinalIgnoreCase))
            return new AcceptLegResult(false, $"PackId mismatch: expected {PackId}, got {leg.PackId}");

        var correlationId = leg.LegId.ToString("N");
        var payload = _codec.EncodeLeg(leg);
        var send = await _transport.SendAsync(
            new ExternalSendRequest(PackId, _options.BaseUrl, payload, correlationId),
            ct).ConfigureAwait(false);

        await LogMessageAsync(InterfaceLogDirection.Out, payload, correlationId, send.Success, ct)
            .ConfigureAwait(false);

        return send.Success
            ? new AcceptLegResult(true)
            : new AcceptLegResult(false, send.ErrorMessage ?? "External transport failed");
    }

    /// <summary>供应商回调入口：解码报文并推进总线 Leg 事件。</summary>
    public async Task HandleCallbackAsync(string payload, CancellationToken ct = default)
    {
        var callback = _codec.DecodeCallback(payload);
        if (callback == null)
            return;

        var correlationId = callback.CorrelationId ?? callback.LegId.ToString("N");
        await LogMessageAsync(InterfaceLogDirection.In, payload, correlationId, true, ct)
            .ConfigureAwait(false);

        if (_bus != null)
        {
            await _bus.OnLegEventAsync(
                new LegEvent(callback.LegId, callback.EventType, Message: callback.Message),
                ct).ConfigureAwait(false);
        }
    }

    public Task CancelLegAsync(Guid legId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default)
        => Task.FromResult<LegStatusDto?>(null);

    public Task<PackHealthDto> HealthAsync(CancellationToken ct = default)
        => Task.FromResult(_options.Enabled
            ? new PackHealthDto(PackId, true, true)
            : new PackHealthDto(PackId, false, false, "External WCS pack is disabled"));

    private async Task LogMessageAsync(
        InterfaceLogDirection direction,
        string payload,
        string? correlationId,
        bool success,
        CancellationToken ct)
    {
        _db.ExtMessageLogs.Add(new ExtMessageLog
        {
            SystemPackId = PackId,
            Direction = direction,
            Payload = payload,
            CorrelationId = correlationId,
            Success = success,
            CreateDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
