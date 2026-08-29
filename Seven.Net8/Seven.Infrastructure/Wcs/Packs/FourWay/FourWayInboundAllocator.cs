using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>四向入库分配：SelectLayer → SelectAisle → SelectLocation + Booking。</summary>
public sealed class FourWayInboundAllocator : IWcsLocationAllocator
{
    private readonly SevenDbContext _db;

    public FourWayInboundAllocator(SevenDbContext db) => _db = db;

    public string PackId => WcsPackIds.FourWay;

    public async Task<AllocationResult> AllocateInboundAsync(AllocationRequest request, CancellationToken ct = default)
    {
        var stages = new List<AllocationStageResult>();
        var height = (int)request.Height;
        var weight = request.Weight;

        var layerCode = await ResolveLayerAsync(request, height, weight, ct);
        if (layerCode == null)
            return new AllocationResult(false, null, null, null, "无可用层策略", stages);
        stages.Add(new AllocationStageResult("Layer", layerCode));

        var aisleCode = await ResolveAisleAsync(request, layerCode, height, weight, ct);
        if (aisleCode == null)
            return new AllocationResult(false, null, layerCode, null, "无可用巷道策略", stages);
        stages.Add(new AllocationStageResult("Aisle", aisleCode));

        var locCode = await SelectAndBookLocationAsync(request.WarehouseId, layerCode, aisleCode, ct);
        if (locCode == null)
            return new AllocationResult(false, null, layerCode, aisleCode, "巷道内无空闲货位", stages);
        stages.Add(new AllocationStageResult("Location", locCode));

        return new AllocationResult(true, locCode, layerCode, aisleCode, null, stages);
    }

    /// <summary>SUDR 层申请：选层（含 Preferred / 权重轮转）。</summary>
    public Task<string?> SelectLayerForRequestAsync(AllocationRequest request, CancellationToken ct = default)
        => ResolveLayerAsync(request, (int)request.Height, request.Weight, ct);

    /// <summary>SUDR 巷申请：在已定层上选巷。</summary>
    public Task<string?> SelectAisleForRequestAsync(
        AllocationRequest request,
        string layerCode,
        CancellationToken ct = default)
        => ResolveAisleAsync(request, layerCode, (int)request.Height, request.Weight, ct);

    /// <summary>SUDR 货位申请：选位并 Booking。</summary>
    public Task<string?> SelectAndBookLocationForRequestAsync(
        int warehouseId,
        string layerCode,
        string aisleCode,
        CancellationToken ct = default)
        => SelectAndBookLocationAsync(warehouseId, layerCode, aisleCode, ct);

    /// <summary>巷策略上的 Ep 目的地点。</summary>
    public async Task<string?> GetAisleDestinationPointAsync(
        string layerCode,
        string aisleCode,
        CancellationToken ct = default)
    {
        var policy = await _db.FwAislePolicies.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.LayerCode == layerCode
                && x.AisleCode == aisleCode
                && x.IsAvailable, ct);
        return string.IsNullOrWhiteSpace(policy?.DestinationPointCode)
            ? null
            : policy!.DestinationPointCode;
    }

    private async Task<string?> ResolveLayerAsync(
        AllocationRequest request,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.PreferredLayerCode))
        {
            var preferred = request.PreferredLayerCode.Trim();
            var layer = await _db.WmsLayers.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == request.WarehouseId
                    && x.PackId == WcsPackIds.FourWay
                    && x.Code == preferred, ct);
            if (layer == null)
                return null;
            if (!layer.IsAvailable)
                return null;
            await TouchAssignmentAsync(FwAssignmentScopeType.Layer, preferred, ct);
            return preferred;
        }

        return await SelectLayerAsync(request, height, weight, ct);
    }

    private async Task<string?> SelectLayerAsync(
        AllocationRequest request,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        var warehouse = await _db.WmsWarehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.WarehouseId, ct);
        if (warehouse == null)
            return null;

        var policies = await _db.FwLayerPolicies
            .Where(x => x.IsAvailable
                        && height <= x.MaxHeight
                        && weight <= x.MaxWeight
                        && x.WarehouseCode == warehouse.Code)
            .ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.ZoneCode))
            policies = policies.Where(x => x.ZoneCode == request.ZoneCode).ToList();
        if (policies.Count == 0)
            return null;

        var layerCodes = policies.Select(x => x.LayerCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var layerRows = await _db.WmsLayers.AsNoTracking()
            .Where(x => x.WarehouseId == request.WarehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && layerCodes.Contains(x.Code))
            .ToListAsync(ct);
        var layerByCode = layerRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var hasLayerMaster = layerRows.Count > 0;

        var candidates = new List<FwLayerPolicy>();
        foreach (var p in policies)
        {
            if (hasLayerMaster || layerByCode.ContainsKey(p.LayerCode))
            {
                if (layerByCode.TryGetValue(p.LayerCode, out var layer) && !layer.IsAvailable)
                    continue;
                if (hasLayerMaster && !layerByCode.ContainsKey(p.LayerCode))
                    continue;
            }

            candidates.Add(p);
        }

        if (candidates.Count == 0)
            return null;

        var records = await _db.FwAssignmentRecords
            .Where(x => x.ScopeType == FwAssignmentScopeType.Layer && layerCodes.Contains(x.ScopeCode))
            .ToListAsync(ct);
        var lastByLayer = records.ToDictionary(x => x.ScopeCode, StringComparer.OrdinalIgnoreCase);

        var chosen = candidates
            .OrderByDescending(p => p.AllocationWeight)
            .ThenBy(p => lastByLayer.TryGetValue(p.LayerCode, out var rec) ? rec.LastAssignedAt : DateTime.MinValue)
            .ThenBy(p => p.LayerCode, StringComparer.OrdinalIgnoreCase)
            .First();

        await TouchAssignmentAsync(FwAssignmentScopeType.Layer, chosen.LayerCode, ct);
        return chosen.LayerCode;
    }

    private async Task<string?> ResolveAisleAsync(
        AllocationRequest request,
        string layerCode,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.PreferredAisleCode))
        {
            var preferred = request.PreferredAisleCode.Trim();
            await TouchAssignmentAsync(FwAssignmentScopeType.Aisle, AisleScopeCode(layerCode, preferred), ct);
            return preferred;
        }

        var hasAislePolicy = await _db.FwAislePolicies.AsNoTracking()
            .AnyAsync(x => x.LayerCode == layerCode, ct);
        if (hasAislePolicy)
            return await SelectAisleAsync(request.WarehouseId, layerCode, height, weight, ct);

        // 无巷策略时：从层内空闲货位推导巷道（兼容 PreferredLayer 仅指定层）
        return await FallbackAisleFromFreeLocationsAsync(request.WarehouseId, layerCode, ct);
    }

    private async Task<string?> FallbackAisleFromFreeLocationsAsync(
        int warehouseId,
        string layerCode,
        CancellationToken ct)
    {
        var layer = await _db.WmsLayers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId
                && x.PackId == WcsPackIds.FourWay
                && x.Code == layerCode, ct);

        var query = _db.WmsLocations.AsNoTracking().Where(x =>
            x.WarehouseId == warehouseId
            && x.PackId == WcsPackIds.FourWay
            && x.Aisle != null
            && !x.IsOccupied && !x.IsLocked && !x.IsBooked);
        if (layer != null)
            query = query.Where(x => x.LayerId == null || x.LayerId == layer.Id);

        var aisle = await query.OrderBy(x => x.Code).Select(x => x.Aisle).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(aisle))
            return null;

        await TouchAssignmentAsync(FwAssignmentScopeType.Aisle, AisleScopeCode(layerCode, aisle), ct);
        return aisle;
    }

    private async Task<string?> SelectAisleAsync(
        int warehouseId,
        string layerCode,
        int height,
        decimal weight,
        CancellationToken ct)
    {
        var policies = await _db.FwAislePolicies
            .Where(x => x.LayerCode == layerCode
                        && x.IsAvailable
                        && height <= x.MaxHeight
                        && weight <= x.MaxWeight)
            .ToListAsync(ct);
        if (policies.Count == 0)
            return null;

        var aisleCodes = policies.Select(x => x.AisleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var layer = await _db.WmsLayers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId
                && x.PackId == WcsPackIds.FourWay
                && x.Code == layerCode, ct);

        // 按当前层过滤，避免跨层同巷道 Code 导致 ToDictionary 冲突 / 空闲数串层
        var aisleQuery = _db.WmsAisles.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && aisleCodes.Contains(x.Code));
        if (layer != null)
            aisleQuery = aisleQuery.Where(x => x.LayerId == layer.Id);
        var aisleRows = await aisleQuery.ToListAsync(ct);
        var aisleByCode = aisleRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var emptyQuery = _db.WmsLocations.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && x.Aisle != null
                        && aisleCodes.Contains(x.Aisle)
                        && !x.IsOccupied && !x.IsLocked && !x.IsBooked);
        if (layer != null)
            emptyQuery = emptyQuery.Where(x => x.LayerId == null || x.LayerId == layer.Id);
        var emptyCounts = await emptyQuery
            .GroupBy(x => x.Aisle!)
            .Select(g => new { Aisle = g.Key, Cnt = g.Count() })
            .ToListAsync(ct);
        var emptyByAisle = emptyCounts.ToDictionary(x => x.Aisle, x => x.Cnt, StringComparer.OrdinalIgnoreCase);

        var locationMasterQuery = _db.WmsLocations.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId
                        && x.PackId == WcsPackIds.FourWay
                        && x.Aisle != null
                        && aisleCodes.Contains(x.Aisle));
        if (layer != null)
            locationMasterQuery = locationMasterQuery.Where(x => x.LayerId == null || x.LayerId == layer.Id);
        var hasLocationMaster = await locationMasterQuery.AnyAsync(ct);

        var candidates = new List<FwAislePolicy>();
        foreach (var p in policies)
        {
            if (aisleByCode.TryGetValue(p.AisleCode, out var aisle) && !aisle.IsAvailable)
                continue;

            if (hasLocationMaster && p.MinEmptySlots > 0)
            {
                emptyByAisle.TryGetValue(p.AisleCode, out var empty);
                if (empty < p.MinEmptySlots)
                    continue;
            }

            // MaxShuttleCount > 0：该层该巷 Reserved+Occupied ≥ 上限则跳过
            if (p.MaxShuttleCount > 0)
            {
                var busy = await _db.FwParkingLedgers.AsNoTracking()
                    .CountAsync(x =>
                        x.LayerCode == p.LayerCode
                        && x.AisleCode == p.AisleCode
                        && (x.Status == FwParkingStatus.Reserved || x.Status == FwParkingStatus.Occupied), ct);
                if (busy >= p.MaxShuttleCount)
                    continue;
            }

            candidates.Add(p);
        }

        if (candidates.Count == 0)
            return null;

        var scopeCodes = aisleCodes.Select(a => AisleScopeCode(layerCode, a)).ToList();
        var records = await _db.FwAssignmentRecords
            .Where(x => x.ScopeType == FwAssignmentScopeType.Aisle && scopeCodes.Contains(x.ScopeCode))
            .ToListAsync(ct);
        var lastByScope = records.ToDictionary(x => x.ScopeCode, StringComparer.OrdinalIgnoreCase);

        var chosen = candidates
            .OrderByDescending(p => p.AllocationWeight)
            .ThenBy(p =>
            {
                var key = AisleScopeCode(layerCode, p.AisleCode);
                return lastByScope.TryGetValue(key, out var rec) ? rec.LastAssignedAt : DateTime.MinValue;
            })
            .ThenBy(p => p.AisleCode, StringComparer.OrdinalIgnoreCase)
            .First();

        await TouchAssignmentAsync(FwAssignmentScopeType.Aisle, AisleScopeCode(layerCode, chosen.AisleCode), ct);
        return chosen.AisleCode;
    }

    private async Task<string?> SelectAndBookLocationAsync(
        int warehouseId,
        string layerCode,
        string aisleCode,
        CancellationToken ct)
    {
        var layer = await _db.WmsLayers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId
                && x.PackId == WcsPackIds.FourWay
                && x.Code == layerCode, ct);

        var query = _db.WmsLocations.Where(x =>
            x.WarehouseId == warehouseId
            && x.PackId == WcsPackIds.FourWay
            && x.Aisle == aisleCode
            && !x.IsOccupied && !x.IsLocked && !x.IsBooked);

        if (layer != null)
            query = query.Where(x => x.LayerId == null || x.LayerId == layer.Id);

        var loc = await query
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
        if (loc == null)
            return null;

        loc.IsBooked = true;
        loc.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return loc.Code;
    }

    private async Task TouchAssignmentAsync(FwAssignmentScopeType scopeType, string scopeCode, CancellationToken ct)
    {
        var record = await _db.FwAssignmentRecords
            .FirstOrDefaultAsync(x => x.ScopeType == scopeType && x.ScopeCode == scopeCode, ct);
        if (record == null)
        {
            record = new FwAssignmentRecord
            {
                ScopeType = scopeType,
                ScopeCode = scopeCode,
                AssignCount = 0,
                CreateDate = DateTime.UtcNow
            };
            _db.FwAssignmentRecords.Add(record);
        }

        record.LastAssignedAt = DateTime.UtcNow;
        record.AssignCount++;
        record.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static string AisleScopeCode(string layerCode, string aisleCode)
        => $"{layerCode}/{aisleCode}";
}
