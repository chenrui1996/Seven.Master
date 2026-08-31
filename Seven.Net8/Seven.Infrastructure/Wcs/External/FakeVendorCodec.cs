using System.Text.Json;
using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.External;

/// <summary>测试/演示用 Codec：Leg ↔ JSON。</summary>
public sealed class FakeVendorCodec : IVendorCodec
{
    public string CodecName => "Fake";

    public string EncodeLeg(TransportLegDto leg)
        => JsonSerializer.Serialize(new
        {
            legId = leg.LegId,
            orderId = leg.OrderId,
            fromCode = leg.FromCode,
            toCode = leg.ToCode,
            containerCode = leg.ContainerCode
        });

    public LegCallbackDto? DecodeCallback(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (!root.TryGetProperty("legId", out var legProp))
                return null;

            var legId = legProp.GetGuid();
            var eventType = LegEventType.Completed;
            if (root.TryGetProperty("eventType", out var evtProp))
            {
                var evtText = evtProp.GetString();
                if (!string.IsNullOrWhiteSpace(evtText) && Enum.TryParse<LegEventType>(evtText, true, out var parsed))
                    eventType = parsed;
            }

            var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
            var correlationId = root.TryGetProperty("correlationId", out var corrProp) ? corrProp.GetString() : null;
            return new LegCallbackDto(legId, eventType, message, correlationId);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string EncodeCallback(Guid legId, LegEventType eventType, string? message = null)
        => JsonSerializer.Serialize(new { legId, eventType = eventType.ToString(), message });
}
