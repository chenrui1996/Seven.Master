using Seven.Domain.Entities.Wms;

namespace Seven.Application.Business;

/// <summary>
/// 标准 WMS 流程扩展钩子。默认 NoOp；项目在 Seven.Business 实现并替换 DI。
/// 见 doc/23 §8。禁止在钩子内直接改 Wms_Stock.Qty。
/// </summary>
public interface IWmsExtensionHooks
{
    Task BeforeInboundApproveAsync(WmsInboundOrder order, CancellationToken ct = default);
    Task AfterInboundApproveAsync(WmsInboundOrder order, CancellationToken ct = default);

    Task BeforeOutboundApproveAsync(WmsOutboundOrder order, CancellationToken ct = default);
    Task AfterOutboundApproveAsync(WmsOutboundOrder order, CancellationToken ct = default);

    /// <summary>过滤待拣选任务（ListPending / PDA）。</summary>
    Task<IReadOnlyList<WmsPickingTask>> FilterPendingPickingAsync(
        IReadOnlyList<WmsPickingTask> tasks,
        CancellationToken ct = default);

    /// <summary>过滤 PDA 待收货入库单 Id 列表（按顺序保留）。</summary>
    Task<IReadOnlyList<int>> FilterPendingInboundOrderIdsAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct = default);
}
