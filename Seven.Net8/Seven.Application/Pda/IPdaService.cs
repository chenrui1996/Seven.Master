using Seven.Domain.Enums;

namespace Seven.Application.Pda;

public record PdaMenuItemDto(string Code, string Title, string Path, string? Permission = null);

public record PdaInboundPendingDto(
    int Id,
    string OrderNo,
    WmsOrderStatus Status,
    IReadOnlyList<PdaInboundLineDto> Lines);

public record PdaInboundLineDto(
    int LineNo,
    string MaterialCode,
    decimal Qty,
    decimal RemainingQty,
    string? ContainerCode,
    string? FromLocation,
    string? ToLocation);

public record PdaReceiveRequest(
    int LineNo,
    decimal Qty,
    string ContainerCode,
    string ReceiveLocationCode);

public record PdaReceiveResult(
    int DetailId,
    int DetailNo,
    string ContainerCode,
    string ReceiveLocationCode,
    string? TargetLocationCode,
    WmsInboundDetailStatus Status);

public record PdaPutawayPendingDto(
    int DetailId,
    int OrderId,
    string OrderNo,
    int DetailNo,
    string MaterialCode,
    decimal Qty,
    string ContainerCode,
    string ReceiveLocationCode,
    string? TargetLocationCode);

public record PdaPutawayConfirmRequest(
    string ContainerCode,
    string ToLocationCode,
    int? DetailId = null);

public record PdaPutawayConfirmResult(
    int DetailId,
    string ContainerCode,
    string FromLocationCode,
    string ToLocationCode);

public record PdaCycleCountPendingDto(
    int Id,
    string OrderNo,
    WmsOrderStatus Status,
    int TotalLines,
    int CountedLines);

public record PdaCycleCountLineDto(
    int LineNo,
    string LocationCode,
    string MaterialCode,
    string? ContainerCode,
    decimal BookQty,
    decimal CountQty,
    decimal DiffQty,
    bool Counted);

public record PdaCycleCountDetailDto(
    int Id,
    string OrderNo,
    WmsOrderStatus Status,
    IReadOnlyList<PdaCycleCountLineDto> Lines);

public record PdaCycleCountRecordRequest(
    int LineNo,
    decimal CountQty,
    string? LocationCode = null,
    string? ContainerCode = null);

public record PdaCycleCountRecordResult(
    int OrderId,
    int LineNo,
    decimal BookQty,
    decimal CountQty,
    decimal DiffQty,
    bool AllCounted);

public record PdaPickingPendingDto(
    int Id,
    string TaskNo,
    int OutboundOrderId,
    string OrderNo,
    string MaterialCode,
    decimal BookQty,
    decimal PickQty,
    string? FromLocation,
    string? ToLocation,
    string? ContainerCode,
    int Status);

public record PdaConfirmPickRequest(
    int PickingTaskId,
    decimal? PickQty = null,
    string? ContainerCode = null,
    string? FromLocation = null);

public interface IPdaService
{
    IReadOnlyList<PdaMenuItemDto> GetMenu();
    Task<IReadOnlyList<PdaInboundPendingDto>> GetPendingInboundAsync(CancellationToken ct = default);
    Task<PdaReceiveResult> ReceiveFloorAsync(int orderId, PdaReceiveRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PdaPutawayPendingDto>> GetPendingPutawayAsync(CancellationToken ct = default);
    Task<PdaPutawayConfirmResult> ConfirmPutawayAsync(PdaPutawayConfirmRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PdaCycleCountPendingDto>> GetPendingCycleCountsAsync(CancellationToken ct = default);
    Task<PdaCycleCountDetailDto> GetCycleCountAsync(int orderId, CancellationToken ct = default);
    Task<PdaCycleCountRecordResult> RecordCycleCountAsync(int orderId, PdaCycleCountRecordRequest request, CancellationToken ct = default);
    Task ConfirmCycleCountAsync(int orderId, CancellationToken ct = default);
    Task<IReadOnlyList<PdaPickingPendingDto>> GetPendingPickingAsync(CancellationToken ct = default);
    Task ConfirmPickAsync(PdaConfirmPickRequest request, CancellationToken ct = default);
}
