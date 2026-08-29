namespace Seven.Application.Wcs;

public record TransportLegDto(
    Guid LegId,
    Guid OrderId,
    string PackId,
    int Seq,
    string FromCode,
    string ToCode,
    string ContainerCode,
    string? HandoverIn,
    string? HandoverOut);

public enum LegEventType
{
    Progress,
    Completed,
    Failed,
    Cancelled,
    DestinationRequest
}

public record LegEvent(
    Guid LegId,
    LegEventType EventType,
    string? Status = null,
    string? Message = null);

public record AcceptLegResult(bool Accepted, string? RejectReason = null);

public record DestinationRequestTrigger(
    string ContainerCode,
    string SourcePointCode,
    int Height,
    int Weight,
    string CheckResult);

public record DispatchDestinationCommand(
    string ContainerCode,
    string DestinationPointCode,
    Guid? LegId = null);

public record DispatchMoveCommand(
    string ContainerCode,
    string FromPointCode,
    string ToPointCode,
    Guid? LegId = null);

public record DeviceSegmentFeedback(
    string ContainerCode,
    string SegmentPointCode,
    string FeedbackCode,
    Guid? LegId = null);

public record CreateTransportOrderRequest(
    string ContainerCode,
    string FromLocationCode,
    string ToLocationCode,
    string? RefType = null,
    string? RefId = null);

public record LegStatusDto(
    Guid LegId,
    string Status,
    string? Message = null);

public record PackHealthDto(
    string PackId,
    bool IsHealthy,
    bool CanAcceptLegs,
    string? Message = null);
