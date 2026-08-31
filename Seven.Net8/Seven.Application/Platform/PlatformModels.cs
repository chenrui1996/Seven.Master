using Seven.Domain.Enums;

namespace Seven.Application.Platform;

public record InterfaceLogWriteRequest(
    InterfaceLogDirection Direction,
    string SystemCode,
    int DurationMs,
    bool Success,
    string? CorrelationId = null,
    Guid? LegId = null,
    string? OrderNo = null,
    string? Path = null,
    string? RequestBody = null,
    string? ResponseBody = null,
    string? ErrorMessage = null);

public record ControlModeState(
    string Scope,
    WcsControlMode Mode,
    bool EStop,
    DateTime UpdatedAt);

public static class ControlScopes
{
    public const string Global = "Global";
}
