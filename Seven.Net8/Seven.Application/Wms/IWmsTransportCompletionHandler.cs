namespace Seven.Application.Wms;

/// <summary>运输单全部 Leg 完成后的 WMS 落账回调。</summary>
public interface IWmsTransportCompletionHandler
{
    Task OnTransportCompletedAsync(Guid orderId, CancellationToken ct = default);
}
