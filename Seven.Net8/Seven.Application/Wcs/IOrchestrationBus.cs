namespace Seven.Application.Wcs;

/// <summary>薄编排总线：跨包运输单与 Leg 事件归一入口。</summary>
public interface IOrchestrationBus
{
    Task<Guid> CreateTransportOrderAsync(CreateTransportOrderRequest req, CancellationToken ct = default);
    Task OnLegEventAsync(LegEvent evt, CancellationToken ct = default);
}
