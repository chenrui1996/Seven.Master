using Microsoft.EntityFrameworkCore;
using Seven.Application.Pda;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Pda;

public sealed class PdaService : IPdaService
{
    private readonly SevenDbContext _db;
    private readonly IInboundOrderService _inbound;
    private readonly IStockService _stock;
    private readonly ICycleCountService _cycleCount;

    public PdaService(
        SevenDbContext db,
        IInboundOrderService inbound,
        IStockService stock,
        ICycleCountService cycleCount)
    {
        _db = db;
        _inbound = inbound;
        _stock = stock;
        _cycleCount = cycleCount;
    }

    public IReadOnlyList<PdaMenuItemDto> GetMenu() =>
    [
        new("receive", "平库收货", "/pages/receive/index", "Pda.Receive"),
        new("putaway", "平库上架", "/pages/putaway/index", "Pda.Putaway"),
        new("cyclecount", "盘点录入", "/pages/cyclecount/index", "Pda.CycleCount")
    ];

    public async Task<IReadOnlyList<PdaInboundPendingDto>> GetPendingInboundAsync(CancellationToken ct = default)
    {
        var orders = await _db.WmsInboundOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Status == WmsOrderStatus.Approved || x.Status == WmsOrderStatus.Executing)
            .OrderByDescending(x => x.Id)
            .Take(50)
            .ToListAsync(ct);

        return orders
            .Select(o => new PdaInboundPendingDto(
                o.Id,
                o.OrderNo,
                o.Status,
                o.Lines
                    .Where(l => l.CompletedQty < l.Qty)
                    .OrderBy(l => l.LineNo)
                    .Select(l => new PdaInboundLineDto(
                        l.LineNo,
                        l.MaterialCode,
                        l.Qty,
                        l.Qty - l.CompletedQty,
                        l.ContainerCode,
                        l.FromLocation,
                        l.ToLocation))
                    .ToList()))
            .Where(o => o.Lines.Count > 0)
            .ToList();
    }

    public async Task<PdaReceiveResult> ReceiveFloorAsync(
        int orderId,
        PdaReceiveRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ContainerCode))
            throw new WmsDomainException("请扫描容器号");
        if (string.IsNullOrWhiteSpace(request.ReceiveLocationCode))
            throw new WmsDomainException("请扫描收货库位");

        // 平库：收货即入账到收货位，不上自动运输；上架由 PDA 确认
        var detail = await _inbound.BuildPalletAsync(orderId, new BuildPalletRequest(
            request.LineNo,
            request.Qty,
            request.ContainerCode.Trim(),
            request.ReceiveLocationCode.Trim(),
            TargetLocationCode: request.ReceiveLocationCode.Trim(),
            AllocateTarget: false), ct);

        return new PdaReceiveResult(
            detail.Id,
            detail.DetailNo,
            detail.ContainerCode,
            detail.ReceiveLocationCode,
            detail.TargetLocationCode,
            detail.Status);
    }

    public async Task<IReadOnlyList<PdaPutawayPendingDto>> GetPendingPutawayAsync(CancellationToken ct = default)
    {
        var details = await _db.WmsInboundDetails.AsNoTracking()
            .Include(x => x.Order)
            .Where(x => x.Status == WmsInboundDetailStatus.Created
                        || x.Status == WmsInboundDetailStatus.Completed)
            .OrderByDescending(x => x.Id)
            .Take(100)
            .ToListAsync(ct);

        var pending = new List<PdaPutawayPendingDto>();
        foreach (var d in details)
        {
            var stock = await _db.WmsStocks.AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ContainerCode == d.ContainerCode
                    && s.LocationCode == d.ReceiveLocationCode
                    && s.Qty > 0, ct);
            if (stock == null) continue;

            // 仍停在收货位 → 待上架
            pending.Add(new PdaPutawayPendingDto(
                d.Id,
                d.OrderId,
                d.Order?.OrderNo ?? string.Empty,
                d.DetailNo,
                d.MaterialCode,
                d.Qty,
                d.ContainerCode,
                d.ReceiveLocationCode,
                d.TargetLocationCode));
        }

        return pending;
    }

    public async Task<PdaPutawayConfirmResult> ConfirmPutawayAsync(
        PdaPutawayConfirmRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ContainerCode))
            throw new WmsDomainException("请扫描容器号");
        if (string.IsNullOrWhiteSpace(request.ToLocationCode))
            throw new WmsDomainException("请扫描目标库位");

        var container = request.ContainerCode.Trim();
        var toLoc = request.ToLocationCode.Trim();

        var detail = request.DetailId is int id
            ? await _db.WmsInboundDetails.FirstOrDefaultAsync(x => x.Id == id, ct)
            : await _db.WmsInboundDetails
                .Where(x => x.ContainerCode == container)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);

        if (detail == null)
            throw new WmsDomainException($"未找到组盘明细: {container}");

        var fromLoc = detail.ReceiveLocationCode;
        if (string.Equals(fromLoc, toLoc, StringComparison.OrdinalIgnoreCase))
            throw new WmsDomainException("目标库位不能与收货位相同");

        var stock = await _db.WmsStocks
            .FirstOrDefaultAsync(s =>
                s.ContainerCode == container
                && s.LocationCode == fromLoc
                && s.Qty > 0, ct)
            ?? throw new WmsDomainException($"收货位无此容器库存: {fromLoc}/{container}");

        var qty = stock.Qty;
        if (stock.AvailableQty < qty)
            stock.AvailableQty = qty;

        await _stock.ShipAsync(new ShipStockRequest(
            fromLoc,
            stock.MaterialCode,
            qty,
            container,
            Reason: "FloorPutaway",
            RefType: "InboundDetail",
            RefId: detail.Id.ToString()), ct);
        await _stock.ReceiveAsync(new ReceiveStockRequest(
            toLoc,
            stock.MaterialCode,
            qty,
            container,
            Reason: "FloorPutaway",
            RefType: "InboundDetail",
            RefId: detail.Id.ToString()), ct);

        detail.TargetLocationCode = toLoc;
        detail.Status = WmsInboundDetailStatus.Completed;
        detail.ModifyDate = DateTime.UtcNow;

        var containerRow = await _db.WmsContainers.FirstOrDefaultAsync(c => c.Code == container, ct);
        if (containerRow != null)
        {
            containerRow.LocationCode = toLoc;
            containerRow.ModifyDate = DateTime.UtcNow;
        }

        var toLocation = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == toLoc, ct);
        if (toLocation != null)
        {
            toLocation.IsOccupied = true;
            toLocation.IsBooked = false;
            toLocation.CurrentContainerCode = container;
            toLocation.ModifyDate = DateTime.UtcNow;
        }

        var fromLocation = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == fromLoc, ct);
        if (fromLocation != null)
        {
            fromLocation.IsOccupied = false;
            fromLocation.CurrentContainerCode = null;
            fromLocation.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return new PdaPutawayConfirmResult(detail.Id, container, fromLoc, toLoc);
    }

    public async Task<IReadOnlyList<PdaCycleCountPendingDto>> GetPendingCycleCountsAsync(CancellationToken ct = default)
    {
        var orders = await _db.WmsCycleCounts.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Status == WmsOrderStatus.Draft || x.Status == WmsOrderStatus.Executing)
            .OrderByDescending(x => x.Id)
            .Take(50)
            .ToListAsync(ct);

        return orders
            .Select(o => new PdaCycleCountPendingDto(
                o.Id,
                o.OrderNo,
                o.Status,
                o.Lines.Count,
                o.Lines.Count(l => l.Counted)))
            .ToList();
    }

    public async Task<PdaCycleCountDetailDto> GetCycleCountAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _cycleCount.GetAsync(orderId, ct)
            ?? throw new WmsDomainException("盘点单不存在");
        return ToDetailDto(order);
    }

    public async Task<PdaCycleCountRecordResult> RecordCycleCountAsync(
        int orderId,
        PdaCycleCountRecordRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var order = await _db.WmsCycleCounts
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new WmsDomainException("盘点单不存在");

        var line = ResolveCycleLine(order, request);
        await _cycleCount.RecordCountAsync(orderId, line.LineNo, request.CountQty, ct);

        var refreshed = await _cycleCount.GetAsync(orderId, ct)
            ?? throw new WmsDomainException("盘点单不存在");
        var saved = refreshed.Lines.First(x => x.LineNo == line.LineNo);
        return new PdaCycleCountRecordResult(
            orderId,
            saved.LineNo,
            saved.BookQty,
            saved.CountQty,
            saved.DiffQty,
            refreshed.Lines.All(x => x.Counted));
    }

    public Task ConfirmCycleCountAsync(int orderId, CancellationToken ct = default) =>
        _cycleCount.ConfirmAdjustAsync(orderId, ct);

    private static WmsCycleCountLine ResolveCycleLine(WmsCycleCount order, PdaCycleCountRecordRequest request)
    {
        if (request.LineNo > 0)
        {
            return order.Lines.FirstOrDefault(x => x.LineNo == request.LineNo)
                ?? throw new WmsDomainException($"盘点行不存在: {request.LineNo}");
        }

        if (string.IsNullOrWhiteSpace(request.LocationCode))
            throw new WmsDomainException("请指定行号或扫描库位");

        var loc = request.LocationCode.Trim();
        var candidates = order.Lines
            .Where(x => string.Equals(x.LocationCode, loc, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.IsNullOrWhiteSpace(request.ContainerCode))
        {
            candidates = candidates
                .Where(x => string.Equals(x.ContainerCode, request.ContainerCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (candidates.Count == 0)
            throw new WmsDomainException($"盘点单无此库位行: {loc}");
        if (candidates.Count > 1)
            throw new WmsDomainException("同库位多行，请指定容器或行号");
        return candidates[0];
    }

    private static PdaCycleCountDetailDto ToDetailDto(WmsCycleCount order) =>
        new(
            order.Id,
            order.OrderNo,
            order.Status,
            order.Lines.OrderBy(x => x.LineNo)
                .Select(l => new PdaCycleCountLineDto(
                    l.LineNo,
                    l.LocationCode,
                    l.MaterialCode,
                    l.ContainerCode,
                    l.BookQty,
                    l.CountQty,
                    l.DiffQty,
                    l.Counted))
                .ToList());
}
