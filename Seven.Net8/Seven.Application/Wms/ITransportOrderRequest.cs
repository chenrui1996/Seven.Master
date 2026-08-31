namespace Seven.Application.Wms;

/// <summary>入库组盘后请求运输单。开关开启时走编排总线，否则 NoOp。</summary>
public interface ITransportOrderRequest
{
    bool IsEnabled { get; }
    Task<Guid> RequestAsync(TransportOrderHookRequest request, CancellationToken ct = default);
}

public record TransportOrderHookRequest(
    string FromLocationCode,
    string? ToLocationCode,
    string? ContainerCode,
    string RefType,
    string RefId,
    string? WcsGroupNo = null,
    int? WcsPri = null);
