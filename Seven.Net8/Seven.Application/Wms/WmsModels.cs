using Seven.Domain.Enums;

namespace Seven.Application.Wms;

public record ReceiveStockRequest(
    string LocationCode,
    string MaterialCode,
    decimal Qty,
    string? ContainerCode = null,
    string? Lot = null,
    string? Reason = null,
    string? RefType = null,
    string? RefId = null);

public record ShipStockRequest(
    string LocationCode,
    string MaterialCode,
    decimal Qty,
    string? ContainerCode = null,
    string? Lot = null,
    string? Reason = null,
    string? RefType = null,
    string? RefId = null);

public record InboundLineInput(
    int LineNo,
    string MaterialCode,
    decimal Qty,
    string? ContainerCode = null,
    string? FromLocation = null,
    string? ToLocation = null);

public record CreateInboundOrderRequest(
    string OrderNo,
    WmsOrderType OrderType,
    IReadOnlyList<InboundLineInput> Lines);

public record ReceiveAndBuildPalletRequest(
    string? ReceiveLocationCode = null,
    string? PackId = null,
    decimal Height = 0,
    decimal Weight = 0,
    bool AllocateTarget = true);

/// <summary>Detail 级组盘：可分次、可调 Allocator 推荐目标货位。</summary>
public record BuildPalletRequest(
    int LineNo,
    decimal Qty,
    string ContainerCode,
    string? ReceiveLocationCode = null,
    string? TargetLocationCode = null,
    string? PackId = null,
    decimal Height = 0,
    decimal Weight = 0,
    bool AllocateTarget = true);

public record OutboundLineInput(
    int LineNo,
    string MaterialCode,
    decimal Qty,
    string? FromLocation = null,
    string? ToLocation = null,
    string? ContainerCode = null,
    int WcsPri = 0);

public record CreateOutboundOrderRequest(
    string OrderNo,
    WmsOrderType OrderType,
    IReadOnlyList<OutboundLineInput> Lines,
    string? WcsGroupNo = null);

public record CycleCountLineInput(
    int LineNo,
    string LocationCode,
    string MaterialCode,
    string? ContainerCode = null);

public record CreateCycleCountRequest(
    string OrderNo,
    IReadOnlyList<CycleCountLineInput> Lines);
