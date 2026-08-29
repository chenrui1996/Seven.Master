using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wms;

public sealed class InboundOrderService : IInboundOrderService
{
    private readonly SevenDbContext _db;
    private readonly IStockService _stock;
    private readonly ITransportOrderRequest? _transport;
    private readonly IWcsLocationAllocatorResolver? _allocators;

    public InboundOrderService(
        SevenDbContext db,
        IStockService stock,
        ITransportOrderRequest? transport = null,
        IWcsLocationAllocatorResolver? allocators = null)
    {
        _db = db;
        _stock = stock;
        _transport = transport;
        _allocators = allocators;
    }

    public Task<PageGridData<WmsInboundOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.WmsInboundOrders.AsNoTracking(), options, ct);

    public Task<WmsInboundOrder?> GetAsync(int orderId, CancellationToken ct = default) =>
        _db.WmsInboundOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public async Task<WmsInboundOrder> CreateAsync(CreateInboundOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrderNo(request.OrderNo);
        if (request.Lines == null || request.Lines.Count == 0)
            throw new WmsDomainException("入库单至少一行");
        if (await _db.WmsInboundOrders.AnyAsync(x => x.OrderNo == request.OrderNo, ct))
            throw new WmsDomainException($"单号已存在: {request.OrderNo}");

        var order = new WmsInboundOrder
        {
            OrderNo = request.OrderNo.Trim(),
            OrderType = request.OrderType,
            Status = WmsOrderStatus.Draft,
            CreateDate = DateTime.UtcNow
        };
        foreach (var line in request.Lines)
        {
            ValidateQty(line.Qty);
            if (string.IsNullOrWhiteSpace(line.MaterialCode))
                throw new WmsDomainException("物料编码不能为空");
            order.Lines.Add(new WmsInboundOrderLine
            {
                LineNo = line.LineNo,
                MaterialCode = line.MaterialCode,
                Qty = line.Qty,
                ContainerCode = line.ContainerCode,
                FromLocation = line.FromLocation,
                ToLocation = line.ToLocation
            });
        }

        _db.WmsInboundOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task ApproveAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status != WmsOrderStatus.Draft)
            throw new WmsDomainException("仅草稿可审核");
        order.Status = WmsOrderStatus.Approved;
        order.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReceiveAndBuildPalletAsync(
        int orderId,
        ReceiveAndBuildPalletRequest? request = null,
        CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException("仅已审核或执行中的入库单可收货组盘");

        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            var remaining = line.Qty - line.CompletedQty;
            if (remaining <= 0) continue;

            var container = line.ContainerCode;
            if (string.IsNullOrWhiteSpace(container))
                throw new WmsDomainException($"行 {line.LineNo} 整单组盘需要 ContainerCode；分次请用 BuildPallet");

            await BuildPalletAsync(orderId, new BuildPalletRequest(
                line.LineNo,
                remaining,
                container,
                request?.ReceiveLocationCode ?? line.FromLocation,
                line.ToLocation,
                request?.PackId,
                request?.Height ?? 0,
                request?.Weight ?? 0,
                request?.AllocateTarget ?? true), ct);
        }
    }

    public async Task<WmsInboundDetail> BuildPalletAsync(
        int orderId,
        BuildPalletRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateQty(request.Qty);
        if (string.IsNullOrWhiteSpace(request.ContainerCode))
            throw new WmsDomainException("组盘必须指定容器号");

        var order = await LoadAsync(orderId, ct);
        if (order.Status is not (WmsOrderStatus.Approved or WmsOrderStatus.Executing))
            throw new WmsDomainException("仅已审核或执行中的入库单可组盘");

        var line = order.Lines.FirstOrDefault(x => x.LineNo == request.LineNo)
            ?? throw new WmsDomainException($"行不存在: {request.LineNo}");
        var remaining = line.Qty - line.CompletedQty;
        if (request.Qty > remaining)
            throw new WmsDomainException($"组盘数量 {request.Qty} 超过行剩余 {remaining}");

        var receiveLoc = !string.IsNullOrWhiteSpace(request.ReceiveLocationCode)
            ? request.ReceiveLocationCode.Trim()
            : (!string.IsNullOrWhiteSpace(line.FromLocation) ? line.FromLocation! : line.ToLocation ?? string.Empty);
        if (string.IsNullOrWhiteSpace(receiveLoc))
            throw new WmsDomainException($"行 {line.LineNo} 缺少收货库位");

        var packId = request.PackId?.Trim().ToLowerInvariant()
            ?? PackCodeRules.TryDetectPackId(request.TargetLocationCode ?? line.ToLocation ?? string.Empty)
            ?? PackCodeRules.TryDetectPackId(receiveLoc)
            ?? string.Empty;

        var targetLoc = request.TargetLocationCode ?? line.ToLocation;
        string? aisle = null;
        string? layer = null;

        if (string.IsNullOrWhiteSpace(targetLoc) && request.AllocateTarget)
        {
            if (string.IsNullOrEmpty(packId))
                packId = await ResolvePackIdFromWarehouseAsync(receiveLoc, ct);
            var allocated = await AllocateTargetAsync(receiveLoc, packId, request.Height, request.Weight, ct);
            targetLoc = allocated.LocationCode;
            aisle = allocated.AisleCode;
            layer = allocated.LayerCode;
            line.ToLocation ??= targetLoc;
        }
        else if (!string.IsNullOrWhiteSpace(targetLoc)
                 && PackCodeRules.TryDetectPackId(targetLoc) == null
                 && !string.IsNullOrEmpty(packId)
                 && WcsPackIds.IsKnown(packId))
        {
            targetLoc = PackCodeRules.EnsurePrefix(targetLoc, packId);
        }

        if (string.IsNullOrWhiteSpace(targetLoc))
            targetLoc = receiveLoc;
        if (string.IsNullOrEmpty(packId))
            packId = PackCodeRules.TryDetectPackId(targetLoc) ?? PackCodeRules.TryDetectPackId(receiveLoc) ?? WcsPackIds.Stacker;

        WmsInboundDetail? detail = null;
        await WmsTransaction.ExecuteAsync(_db, async () =>
        {
            order.Status = WmsOrderStatus.Executing;
            var maxDetailNo = await _db.WmsInboundDetails
                .Where(x => x.OrderId == order.Id)
                .Select(x => (int?)x.DetailNo)
                .MaxAsync(ct) ?? 0;

            detail = new WmsInboundDetail
            {
                OrderId = order.Id,
                LineId = line.Id,
                DetailNo = maxDetailNo + 1,
                MaterialCode = line.MaterialCode,
                Qty = request.Qty,
                ContainerCode = request.ContainerCode.Trim(),
                ReceiveLocationCode = receiveLoc,
                TargetLocationCode = targetLoc,
                AssignedAisle = aisle,
                AssignedLayer = layer,
                PackId = packId,
                Status = WmsInboundDetailStatus.Created,
                CreateDate = DateTime.UtcNow
            };
            _db.WmsInboundDetails.Add(detail);

            await EnsureContainerAsync(detail.ContainerCode, receiveLoc, ct);
            await _stock.ReceiveAsync(new ReceiveStockRequest(
                receiveLoc,
                line.MaterialCode,
                request.Qty,
                detail.ContainerCode,
                Reason: "InboundReceive",
                RefType: "InboundDetail",
                RefId: order.OrderNo), ct);

            line.CompletedQty += request.Qty;
            if (!string.IsNullOrWhiteSpace(targetLoc)
                && !string.Equals(receiveLoc, targetLoc, StringComparison.OrdinalIgnoreCase))
            {
                await BookLocationAsync(targetLoc, ct);
            }

            var needsTransport = WillRequestTransport(receiveLoc, targetLoc, detail.ContainerCode);
            if (!needsTransport && order.Lines.All(x => x.CompletedQty >= x.Qty))
            {
                detail.Status = WmsInboundDetailStatus.Completed;
                order.Status = WmsOrderStatus.Completed;
            }

            await _db.SaveChangesAsync(ct);
        }, ct);

        detail = detail ?? throw new InvalidOperationException("组盘明细未创建");

        if (WillRequestTransport(receiveLoc, targetLoc, detail.ContainerCode) && _transport != null)
        {
            var transportId = await _transport.RequestAsync(new TransportOrderHookRequest(
                receiveLoc,
                targetLoc,
                detail.ContainerCode,
                "InboundDetail",
                detail.Id.ToString()), ct);
            detail.TransportOrderId = transportId;
            detail.Status = WmsInboundDetailStatus.Transporting;
            detail.ModifyDate = DateTime.UtcNow;
            order.Status = WmsOrderStatus.Executing;
            await _db.SaveChangesAsync(ct);
        }
        else if (detail.Status == WmsInboundDetailStatus.Created
                 && string.Equals(receiveLoc, targetLoc, StringComparison.OrdinalIgnoreCase))
        {
            detail.Status = WmsInboundDetailStatus.Completed;
            if (order.Lines.All(x => x.CompletedQty >= x.Qty))
                order.Status = WmsOrderStatus.Completed;
            await _db.SaveChangesAsync(ct);
        }

        return detail;
    }

    private async Task<(string LocationCode, string? AisleCode, string? LayerCode)> AllocateTargetAsync(
        string receiveLoc,
        string packId,
        decimal height,
        decimal weight,
        CancellationToken ct)
    {
        if (_allocators == null)
            throw new WmsDomainException("未注册货位分配器，无法自动推荐目标库位");

        var warehouseId = await ResolveWarehouseIdAsync(receiveLoc, ct);
        var result = await _allocators.GetAllocator(packId)
            .AllocateInboundAsync(new AllocationRequest(warehouseId, packId, height, weight), ct);
        if (!result.Ok || string.IsNullOrWhiteSpace(result.LocationCode))
            throw new WmsDomainException(result.Message ?? "货位分配失败");
        return (result.LocationCode!, result.AisleCode, result.LayerCode);
    }

    private async Task<string> ResolvePackIdFromWarehouseAsync(string receiveLoc, CancellationToken ct)
    {
        var warehouseId = await ResolveWarehouseIdAsync(receiveLoc, ct);
        var warehouse = await _db.WmsWarehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == warehouseId, ct)
            ?? throw new WmsDomainException("仓库不存在");
        WarehousePackRules.EnsureAtLeastOne(warehouse.EnabledPackIds);
        var packs = WarehousePackRules.Parse(warehouse.EnabledPackIds);
        if (packs.Contains(WcsPackIds.Stacker))
            return WcsPackIds.Stacker;
        return packs[0];
    }

    private async Task<int> ResolveWarehouseIdAsync(string locationCode, CancellationToken ct)
    {
        var loc = await _db.WmsLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == locationCode, ct);
        if (loc != null) return loc.WarehouseId;

        var warehouses = await _db.WmsWarehouses.AsNoTracking().Take(2).ToListAsync(ct);
        if (warehouses.Count == 1) return warehouses[0].Id;
        throw new WmsDomainException($"无法解析库位所属仓库: {locationCode}");
    }

    private async Task BookLocationAsync(string locationCode, CancellationToken ct)
    {
        var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == locationCode, ct);
        if (loc == null) return;
        if (loc.IsOccupied || loc.IsLocked)
            throw new WmsDomainException($"目标货位不可用: {locationCode}");
        loc.IsBooked = true;
        loc.ModifyDate = DateTime.UtcNow;
    }

    private bool WillRequestTransport(string? receiveLoc, string? targetLoc, string? containerCode) =>
        _transport is { IsEnabled: true }
        && !string.IsNullOrWhiteSpace(containerCode)
        && !string.IsNullOrWhiteSpace(receiveLoc)
        && !string.IsNullOrWhiteSpace(targetLoc)
        && !string.Equals(receiveLoc, targetLoc, StringComparison.OrdinalIgnoreCase);

    private async Task<WmsInboundOrder> LoadAsync(int orderId, CancellationToken ct)
    {
        var order = await _db.WmsInboundOrders
            .Include(x => x.Lines)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null)
            throw new WmsDomainException("入库单不存在");
        return order;
    }

    private async Task EnsureContainerAsync(string containerCode, string receiveLoc, CancellationToken ct)
    {
        var exists = await _db.WmsContainers.AnyAsync(c => c.Code == containerCode, ct);
        if (exists) return;
        _db.WmsContainers.Add(new WmsContainer
        {
            Code = containerCode,
            LocationCode = receiveLoc,
            Status = WmsContainerStatus.Occupied,
            CreateDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateOrderNo(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            throw new WmsDomainException("单号不能为空");
    }

    private static void ValidateQty(decimal qty)
    {
        if (qty <= 0)
            throw new WmsDomainException("数量必须大于 0");
    }
}
