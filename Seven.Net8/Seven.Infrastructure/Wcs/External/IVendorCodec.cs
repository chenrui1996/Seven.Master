using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.External;

public record LegCallbackDto(
    Guid LegId,
    LegEventType EventType,
    string? Message = null,
    string? CorrelationId = null);

/// <summary>供应商报文编解码（Leg ↔ 外部格式）。</summary>
public interface IVendorCodec
{
    string CodecName { get; }

    string EncodeLeg(TransportLegDto leg);

    LegCallbackDto? DecodeCallback(string payload);
}
