using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Bus;

/// <summary>薄编排总线：规划 Leg、接单、按事件推进下一段。</summary>
public sealed class OrchestrationBus : IOrchestrationBus
{
    private readonly SevenDbContext _db;
    private readonly IWcsPackResolver _resolver;
    private readonly IWmsTransportCompletionHandler _completion;
    private readonly ILogger<OrchestrationBus>? _logger;

    public OrchestrationBus(
        SevenDbContext db,
        IWcsPackResolver resolver,
        IWmsTransportCompletionHandler completion,
        ILogger<OrchestrationBus>? logger = null)
    {
        _db = db;
        _resolver = resolver;
        _completion = completion;
        _logger = logger;
    }

    public async Task<Guid> CreateTransportOrderAsync(CreateTransportOrderRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.FromLocationCode) || string.IsNullOrWhiteSpace(req.ToLocationCode))
            throw new BusDomainException("起终点库位不能为空");

        var order = new BusTransportOrder
        {
            Id = Guid.NewGuid(),
            ContainerCode = req.ContainerCode ?? string.Empty,
            FromLocationCode = req.FromLocationCode,
            ToLocationCode = req.ToLocationCode,
            Status = BusOrderStatus.Planning,
            RefType = req.RefType,
            RefId = req.RefId,
            CreateDate = DateTime.UtcNow
        };

        var planned = await PlanLegsAsync(order, ct);
        if (planned.Count == 0)
        {
            order.Status = BusOrderStatus.Failed;
            order.FailReason = "Planning failed: no pack or handover for route";
            _db.BusTransportOrders.Add(order);
            await _db.SaveChangesAsync(ct);
            throw new BusDomainException(order.FailReason);
        }

        foreach (var leg in planned)
            order.Legs.Add(leg);

        var first = order.Legs.OrderBy(x => x.Seq).First();
        await AcceptLegAsync(order, first, ct);
        _db.BusTransportOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order.Id;
    }

    public async Task OnLegEventAsync(LegEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);
        var order = await _db.BusTransportOrders
            .Include(x => x.Legs)
            .FirstOrDefaultAsync(x => x.Legs.Any(l => l.Id == evt.LegId), ct);
        if (order == null)
            throw new BusDomainException($"Leg 不存在: {evt.LegId}");

        var leg = order.Legs.Single(x => x.Id == evt.LegId);
        switch (evt.EventType)
        {
            case LegEventType.Progress:
                if (leg.Status is BusLegStatus.Pending or BusLegStatus.Accepted)
                    leg.Status = BusLegStatus.Running;
                if (!string.IsNullOrWhiteSpace(evt.Message))
                    leg.Message = evt.Message;
                break;

            case LegEventType.Completed:
                if (leg.Status is BusLegStatus.Completed)
                    return;
                leg.Status = BusLegStatus.Completed;
                leg.Message = evt.Message;
                await AdvanceOrCompleteAsync(order, ct);
                break;

            case LegEventType.Failed:
                leg.Status = BusLegStatus.Failed;
                leg.Message = evt.Message ?? evt.Status;
                order.Status = BusOrderStatus.Failed;
                order.FailReason = leg.Message;
                break;

            case LegEventType.Cancelled:
                leg.Status = BusLegStatus.Cancelled;
                leg.Message = evt.Message;
                break;
        }

        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>恢复：激活「前一段已完成、本段仍 Pending」的 Leg。</summary>
    public async Task ActivateDuePendingLegsAsync(CancellationToken ct = default)
    {
        var orders = await _db.BusTransportOrders
            .Include(x => x.Legs)
            .Where(x => x.Status == BusOrderStatus.Executing)
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            var ordered = order.Legs.OrderBy(x => x.Seq).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].Status != BusLegStatus.Pending)
                    continue;
                var prevDone = i == 0 || ordered[i - 1].Status == BusLegStatus.Completed;
                if (!prevDone)
                    break;
                await AcceptLegAsync(order, ordered[i], ct);
                await _db.SaveChangesAsync(ct);
                break;
            }
        }
    }

    private async Task AdvanceOrCompleteAsync(BusTransportOrder order, CancellationToken ct)
    {
        var next = order.Legs.OrderBy(x => x.Seq).FirstOrDefault(x => x.Status == BusLegStatus.Pending);
        if (next != null)
        {
            await AcceptLegAsync(order, next, ct);
            return;
        }

        if (order.Legs.All(x => x.Status == BusLegStatus.Completed))
        {
            order.Status = BusOrderStatus.Completed;
            await _db.SaveChangesAsync(ct);
            await _completion.OnTransportCompletedAsync(order.Id, ct);
        }
    }

    private async Task AcceptLegAsync(BusTransportOrder order, BusTransportLeg leg, CancellationToken ct)
    {
        var pack = _resolver.ResolveByPackId(leg.PackId);
        if (pack == null)
        {
            FailAccept(order, leg, $"Pack not registered: {leg.PackId}");
            return;
        }

        var dto = new TransportLegDto(
            leg.Id,
            order.Id,
            leg.PackId,
            leg.Seq,
            leg.FromCode,
            leg.ToCode,
            leg.ContainerCode,
            leg.HandoverIn,
            leg.HandoverOut);
        var result = await pack.AcceptLegAsync(dto, ct);
        if (!result.Accepted)
        {
            FailAccept(order, leg, result.RejectReason ?? "AcceptLeg rejected");
            return;
        }

        leg.Status = BusLegStatus.Accepted;
        order.Status = BusOrderStatus.Executing;
    }

    private static void FailAccept(BusTransportOrder order, BusTransportLeg leg, string reason)
    {
        leg.Status = BusLegStatus.Failed;
        leg.Message = reason;
        order.Status = BusOrderStatus.Failed;
        order.FailReason = reason;
    }

    private async Task<List<BusTransportLeg>> PlanLegsAsync(BusTransportOrder order, CancellationToken ct)
    {
        var packs = _resolver.GetPacks();
        if (packs.Count == 1)
        {
            var only = packs[0];
            if (await only.CanHandleAsync(order.FromLocationCode, order.ToLocationCode, ct))
                return [NewLeg(order, only.PackId, 1, order.FromLocationCode, order.ToLocationCode, null, null)];
            return [];
        }

        if (packs.Count == 0)
            return [];

        var links = await _db.WmsHandoverLinks.AsNoTracking().ToListAsync(ct);
        foreach (var link in links)
        {
            var fromPack = _resolver.ResolveByPackId(link.FromPackId);
            var toPack = _resolver.ResolveByPackId(link.ToPackId);
            if (fromPack == null || toPack == null)
                continue;
            if (!await fromPack.CanHandleAsync(order.FromLocationCode, link.LocationCode, ct))
                continue;
            if (!await toPack.CanHandleAsync(link.LocationCode, order.ToLocationCode, ct))
                continue;

            return
            [
                NewLeg(order, fromPack.PackId, 1, order.FromLocationCode, link.LocationCode, null, link.LocationCode),
                NewLeg(order, toPack.PackId, 2, link.LocationCode, order.ToLocationCode, link.LocationCode, null)
            ];
        }

        _logger?.LogWarning(
            "Planning failed for {From} → {To}: no handover chain among {Count} packs",
            order.FromLocationCode, order.ToLocationCode, packs.Count);
        return [];
    }

    private static BusTransportLeg NewLeg(
        BusTransportOrder order,
        string packId,
        int seq,
        string from,
        string to,
        string? handoverIn,
        string? handoverOut)
        => new()
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PackId = packId,
            Seq = seq,
            FromCode = from,
            ToCode = to,
            ContainerCode = order.ContainerCode,
            HandoverIn = handoverIn,
            HandoverOut = handoverOut,
            Status = BusLegStatus.Pending,
            CreateDate = DateTime.UtcNow
        };
}
