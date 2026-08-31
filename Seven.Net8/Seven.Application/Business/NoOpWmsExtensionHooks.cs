using Seven.Domain.Entities.Wms;

namespace Seven.Application.Business;

/// <summary>默认空钩子（Application 层，供 Infrastructure 在未注入时回退）。</summary>
public sealed class NoOpWmsExtensionHooks : IWmsExtensionHooks
{
    public static NoOpWmsExtensionHooks Instance { get; } = new();

    public Task BeforeInboundApproveAsync(WmsInboundOrder order, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task AfterInboundApproveAsync(WmsInboundOrder order, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task BeforeOutboundApproveAsync(WmsOutboundOrder order, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task AfterOutboundApproveAsync(WmsOutboundOrder order, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<WmsPickingTask>> FilterPendingPickingAsync(
        IReadOnlyList<WmsPickingTask> tasks,
        CancellationToken ct = default) =>
        Task.FromResult(tasks);

    public Task<IReadOnlyList<int>> FilterPendingInboundOrderIdsAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct = default) =>
        Task.FromResult(orderIds);
}
