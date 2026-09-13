using Seven.Domain.Enums;

namespace Seven.Application.Wcs;

public interface IFourWayOpsService
{
    Task<object> GetMetaAsync(CancellationToken ct = default);
    Task<object> GetBoardAsync(CancellationToken ct = default);
    Task<object?> GetTaskTreeAsync(Guid? shuttleTaskId, Guid? putAwayId, Guid? retrievalId, Guid? hoistTaskId, CancellationToken ct = default);
    Task<(bool Ok, string Message, object? Data)> CreateInboundAsync(FourWayOpsInboundRequest req, CancellationToken ct = default);
    Task<object> GetPickableMapAsync(string? layerCode, CancellationToken ct = default);
    Task<(bool Ok, string Message, object? Data)> PointDispatchAsync(FourWayOpsPointDispatchRequest req, CancellationToken ct = default);
    Task<(bool Ok, string Message, object? Data)> ChargeAsync(FourWayOpsChargeRequest req, bool stop, CancellationToken ct = default);
    Task<(bool Ok, string Message)> ForceCompleteAsync(FourWayOpsForceCompleteRequest req, CancellationToken ct = default);
    Task<(bool Ok, string Message)> ResendAsync(FourWayOpsResendRequest req, CancellationToken ct = default);
}

public interface IStackerOpsService
{
    Task<object> GetBoardAsync(CancellationToken ct = default);
    Task<object?> GetTaskTreeAsync(Guid? putAwayId, Guid? retrievalId, Guid? deviceTaskId, CancellationToken ct = default);
    Task<(bool Ok, string Message)> ForceCompleteAsync(StackerOpsForceCompleteRequest req, CancellationToken ct = default);
    Task<(bool Ok, string Message)> ResendAsync(StackerOpsResendRequest req, CancellationToken ct = default);
    Task<(bool Ok, string Message)> SetRequestPointEnabledAsync(int id, bool enabled, CancellationToken ct = default);
    Task<object> ListRequestPointsAsync(CancellationToken ct = default);
}

public record FourWayOpsInboundRequest(
    string ContainerCode,
    string? MaterialName,
    decimal Quantity,
    string GatewayCode,
    string Strategy, // auto | layer | location
    string? LayerCode,
    string? LocationCode,
    string? ShuttleNo,
    bool SyncWms);

public record FourWayOpsPointDispatchRequest(
    string FromCode,
    string ToCode,
    string? ContainerCode,
    string? LayerCode);

public record FourWayOpsChargeRequest(string FromCode, string ChargePointCode, string? ContainerCode);

public record FourWayOpsForceCompleteRequest(
    string TargetType, // putAway | retrieval | shuttle | hoist | hoistExec | path
    Guid Id);

public record FourWayOpsResendRequest(Guid ShuttleTaskId, int? PathSeq);

public record StackerOpsForceCompleteRequest(
    string TargetType, // putAway | retrieval | device
    Guid Id);

public record StackerOpsResendRequest(Guid DeviceTaskId);
