using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Simulator;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Entities.Simulator;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Simulator;

public sealed class SimulationDeployService : ISimulationDeployService
{
    public const string SimWarehousePrefix = "SIM_";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly SevenDbContext _db;

    public SimulationDeployService(SevenDbContext db) => _db = db;

    public ValidateFeaturesResult ValidateFeatures(SimFeaturesDto features)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!features.Wms)
            warnings.Add("工程未勾选 Wms：Deploy 仍会写 Wms_Location（Features 仅影响前端菜单）");

        if (!features.WcsPacks.Stacker && !features.WcsPacks.FourWay && !features.WcsPacks.BoxSort)
            warnings.Add("未启用任何 WCS 包：仅部署库位主数据");

        if (features.WcsPacks.FourWay && !features.HotStore)
            warnings.Add("四向车建议同时开启 HotStore");

        if (features.OrchestrationBus && !features.Wms)
            warnings.Add("OrchestrationBus 建议同时勾选 Wms（前端菜单组合）");

        if (!features.Simulator)
            warnings.Add("工程 meta 未勾选 Simulator");

        return new ValidateFeaturesResult(true, errors, warnings);
    }

    public async Task<DeployResult> DeployAsync(SimProjectDto project, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (string.IsNullOrWhiteSpace(project.Meta.Name))
            throw new InvalidOperationException("工程名称不能为空");
        if (project.Map.Nodes == null || project.Map.Nodes.Count == 0)
            throw new InvalidOperationException("地图至少需要一个节点");

        var validation = ValidateFeatures(project.Meta.Features);
        if (!validation.Ok)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        var packId = string.IsNullOrWhiteSpace(project.Map.PackId)
            ? WcsPackIds.Stacker
            : project.Map.PackId.Trim().ToLowerInvariant();
        if (!WcsPackIds.IsKnown(packId))
            throw new InvalidOperationException($"未知 PackId: {packId}");

        var warehouseCode = SimWarehousePrefix + SanitizeCode(project.Meta.Name);

        var warehouse = await _db.WmsWarehouses.FirstOrDefaultAsync(x => x.Code == warehouseCode, ct);
        if (warehouse == null)
        {
            warehouse = new WmsWarehouse
            {
                Code = warehouseCode,
                Name = $"仿真仓-{project.Meta.Name}",
                EnabledPackIds = packId,
                CreateDate = DateTime.UtcNow
            };
            _db.WmsWarehouses.Add(warehouse);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            warehouse.EnabledPackIds = WarehousePackRules.EnsureContains(warehouse.EnabledPackIds, packId);
            warehouse.ModifyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        int? defaultLayerId = null;
        if (packId == WcsPackIds.FourWay)
            defaultLayerId = await EnsureDefaultFourWayLayerAsync(warehouse.Id, ct);

        var locationCount = 0;
        foreach (var node in project.Map.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Code)) continue;
            var code = PackCodeRules.EnsurePrefix(node.Code.Trim(), packId);
            var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == code, ct);
            if (loc == null)
            {
                loc = new WmsLocation
                {
                    WarehouseId = warehouse.Id,
                    PackId = packId,
                    Code = code,
                    LayerId = defaultLayerId,
                    CreateDate = DateTime.UtcNow
                };
                _db.WmsLocations.Add(loc);
                locationCount++;
            }
            else
            {
                if (string.IsNullOrEmpty(loc.PackId))
                    loc.PackId = packId;
                if (loc.LayerId == null && defaultLayerId != null)
                    loc.LayerId = defaultLayerId;
            }
        }

        await _db.SaveChangesAsync(ct);

        var edgeCount = 0;
        if (packId == WcsPackIds.Stacker)
            edgeCount = await EnsureStackerSeedAsync(project, packId, ct);
        else if (packId == WcsPackIds.FourWay)
            edgeCount = await EnsureFourWayMapAsync(project, packId, ct);

        await EnsureScadaAsync(project, packId, ct);

        var active = await _db.SimDeployments
            .Where(x => x.ProjectName == project.Meta.Name && x.Status == "Deployed")
            .ToListAsync(ct);
        foreach (var row in active)
        {
            row.Status = "Undeployed";
            row.UndeployedAt = DateTime.UtcNow;
            row.ModifyDate = DateTime.UtcNow;
        }

        var deployment = new SimDeployment
        {
            ProjectName = project.Meta.Name.Trim(),
            PackId = packId,
            WarehouseCode = warehouseCode,
            Status = "Deployed",
            LocationCount = project.Map.Nodes.Count,
            EdgeCount = edgeCount,
            ProjectJson = JsonSerializer.Serialize(project, JsonOptions),
            DeployedAt = DateTime.UtcNow,
            CreateDate = DateTime.UtcNow
        };
        _db.SimDeployments.Add(deployment);
        await _db.SaveChangesAsync(ct);

        return new DeployResult(
            deployment.Id,
            warehouseCode,
            project.Map.Nodes.Count,
            edgeCount,
            locationCount > 0
                ? $"已部署：新增库位 {locationCount}，节点总计 {project.Map.Nodes.Count}"
                : $"已部署：库位均已存在，节点总计 {project.Map.Nodes.Count}");
    }

    public async Task UndeployAsync(string projectName, bool removeLocations = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new InvalidOperationException("工程名称不能为空");

        var rows = await _db.SimDeployments
            .Where(x => x.ProjectName == projectName && x.Status == "Deployed")
            .ToListAsync(ct);
        if (rows.Count == 0)
            throw new InvalidOperationException($"未找到已部署工程: {projectName}");

        foreach (var row in rows)
        {
            row.Status = "Undeployed";
            row.UndeployedAt = DateTime.UtcNow;
            row.ModifyDate = DateTime.UtcNow;

            if (removeLocations && !string.IsNullOrWhiteSpace(row.ProjectJson))
            {
                var project = JsonSerializer.Deserialize<SimProjectDto>(row.ProjectJson, JsonOptions);
                var packId = string.IsNullOrWhiteSpace(row.PackId) ? WcsPackIds.Stacker : row.PackId;
                if (project?.Map.Nodes != null)
                {
                    foreach (var node in project.Map.Nodes)
                    {
                        var raw = node.Code?.Trim();
                        if (string.IsNullOrEmpty(raw)) continue;
                        var code = PackCodeRules.EnsurePrefix(raw, packId);
                        var hasStock = await _db.WmsStocks.AnyAsync(s => s.LocationCode == code && s.Qty > 0, ct);
                        if (hasStock) continue;
                        var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == code, ct);
                        if (loc != null)
                            _db.WmsLocations.Remove(loc);
                    }
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetAsync(string projectName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new InvalidOperationException("工程名称不能为空");

        var warehouseCode = SimWarehousePrefix + SanitizeCode(projectName);
        var warehouse = await _db.WmsWarehouses.FirstOrDefaultAsync(x => x.Code == warehouseCode, ct)
            ?? throw new InvalidOperationException($"未找到仿真仓库: {warehouseCode}");

        var locationCodes = await _db.WmsLocations
            .Where(x => x.WarehouseId == warehouse.Id)
            .Select(x => x.Code)
            .ToListAsync(ct);
        var locationSet = locationCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (locationSet.Count == 0)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        bool Touches(string? code) =>
            !string.IsNullOrWhiteSpace(code) && locationSet.Contains(code);

        var openOrderStatuses = new[]
        {
            BusOrderStatus.Created,
            BusOrderStatus.Planning,
            BusOrderStatus.Executing,
            BusOrderStatus.Cancelling
        };
        var orders = await _db.BusTransportOrders
            .Include(x => x.Legs)
            .Where(x => openOrderStatuses.Contains(x.Status))
            .ToListAsync(ct);

        var affectedLegIds = new HashSet<Guid>();
        foreach (var order in orders)
        {
            if (!Touches(order.FromLocationCode) && !Touches(order.ToLocationCode))
                continue;

            order.Status = BusOrderStatus.Failed;
            order.FailReason = "Simulation reset";
            order.ModifyDate = DateTime.UtcNow;

            foreach (var leg in order.Legs)
            {
                affectedLegIds.Add(leg.Id);
                if (leg.Status is BusLegStatus.Completed or BusLegStatus.Failed or BusLegStatus.Cancelled)
                    continue;
                leg.Status = BusLegStatus.Cancelled;
                leg.Message = "Simulation reset";
                leg.ModifyDate = DateTime.UtcNow;
            }
        }

        foreach (var task in await _db.StkPutAwayTasks
                     .Where(x => x.Status != StkPutAwayStatus.Completed
                                 && x.Status != StkPutAwayStatus.Cancelled
                                 && x.Status != StkPutAwayStatus.Failed)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId) && !Touches(task.FromCode) && !Touches(task.ToCode)
                && !Touches(task.AssignedLocationCode))
                continue;
            task.Status = StkPutAwayStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.StkRetrievalTasks
                     .Where(x => x.Status != StkRetrievalStatus.Completed
                                 && x.Status != StkRetrievalStatus.Cancelled
                                 && x.Status != StkRetrievalStatus.Failed
                                 && x.Status != StkRetrievalStatus.Suspended)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId) && !Touches(task.FromCode) && !Touches(task.ToCode))
                continue;
            task.Status = StkRetrievalStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.StkDeviceTasks
                     .Where(x => x.Status != StkDeviceTaskStatus.Completed && x.Status != StkDeviceTaskStatus.Failed)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId)
                && !Touches(task.FromPointCode)
                && !Touches(task.DestinationPointCode))
                continue;
            task.Status = StkDeviceTaskStatus.Failed;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.FwShuttleTasks
                     .Where(x => x.Status != FwShuttleTaskStatus.Completed
                                 && x.Status != FwShuttleTaskStatus.Cancelled
                                 && x.Status != FwShuttleTaskStatus.Failed)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId) && !Touches(task.FromCode) && !Touches(task.ToCode))
                continue;
            task.Status = FwShuttleTaskStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.FwPutAwayTasks
                     .Where(x => x.Status != FwPutAwayStatus.Completed
                                 && x.Status != FwPutAwayStatus.Cancelled
                                 && x.Status != FwPutAwayStatus.Failed)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId) && !Touches(task.FromCode) && !Touches(task.ToCode)
                && !Touches(task.AssignedLocationCode))
                continue;
            task.Status = FwPutAwayStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.FwRetrievalTasks
                     .Where(x => x.Status != FwRetrievalStatus.Completed
                                 && x.Status != FwRetrievalStatus.Cancelled
                                 && x.Status != FwRetrievalStatus.Failed
                                 && x.Status != FwRetrievalStatus.Suspended)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId) && !Touches(task.FromCode) && !Touches(task.ToCode))
                continue;
            task.Status = FwRetrievalStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.FwHoistTasks
                     .Where(x => x.Status != FwHoistTaskStatus.Completed
                                 && x.Status != FwHoistTaskStatus.Cancelled
                                 && x.Status != FwHoistTaskStatus.Failed)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId)
                && !Touches(task.FromCode)
                && !Touches(task.ToCode)
                && !Touches(task.SrcAddress)
                && !Touches(task.DesAddress))
                continue;
            task.Status = FwHoistTaskStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        foreach (var task in await _db.FwHoistExecTasks
                     .Where(x => x.Status != FwHoistExecStatus.Completed
                                 && x.Status != FwHoistExecStatus.Cancelled
                                 && x.Status != FwHoistExecStatus.Failed)
                     .ToListAsync(ct))
        {
            if (!affectedLegIds.Contains(task.LegId)
                && !Touches(task.SrcAddress)
                && !Touches(task.DesAddress))
                continue;
            task.Status = FwHoistExecStatus.Cancelled;
            task.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<SimPromotePreviewResult> PromotePreviewAsync(SimPromoteRequest request, CancellationToken ct = default)
    {
        var devices = ValidatePromoteDevices(request);
        var warnings = await GetPromoteWarningsAsync(request.ProjectName, ct);
        return new SimPromotePreviewResult(
            request.ProjectName.Trim(),
            devices,
            warnings);
    }

    public async Task<SimPromoteResult> PromoteAsync(SimPromoteRequest request, CancellationToken ct = default)
    {
        var devices = ValidatePromoteDevices(request);
        var warnings = await GetPromoteWarningsAsync(request.ProjectName, ct);
        if (warnings.Any(w => w.Contains("未找到已部署工程")))
            throw new InvalidOperationException(warnings.First(w => w.Contains("未找到已部署工程")));

        var deployment = await FindActiveDeploymentAsync(request.ProjectName, ct)
            ?? throw new InvalidOperationException($"未找到已部署工程: {request.ProjectName}");

        var warehouseCode = deployment.WarehouseCode;
        var commCount = await UpsertCommConnectionsAsync(devices, ct);

        var project = string.IsNullOrWhiteSpace(deployment.ProjectJson)
            ? null
            : JsonSerializer.Deserialize<SimProjectDto>(deployment.ProjectJson, JsonOptions);
        if (project != null)
        {
            project = project with
            {
                Meta = project.Meta with { RuntimeMode = "Production" },
                Promote = new SimPromoteDto(devices)
            };
            deployment.ProjectJson = JsonSerializer.Serialize(project, JsonOptions);
        }

        deployment.Status = "Promoted";
        deployment.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new SimPromoteResult(
            deployment.Id,
            deployment.ProjectName,
            warehouseCode,
            deployment.Status,
            commCount,
            commCount > 0
                ? $"已 Promote：写入 {commCount} 条 CommConnection，仓库仍为 {warehouseCode}"
                : $"已 Promote：仓库仍为 {warehouseCode}");
    }

    public async Task<IReadOnlyList<SimDeploymentSummary>> ListDeploymentsAsync(CancellationToken ct = default)
    {
        return await _db.SimDeployments.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(50)
            .Select(x => new SimDeploymentSummary(
                x.Id, x.ProjectName, x.PackId, x.WarehouseCode, x.Status, x.LocationCount, x.DeployedAt))
            .ToListAsync(ct);
    }

    private async Task<int> EnsureDefaultFourWayLayerAsync(int warehouseId, CancellationToken ct)
    {
        var code = PackCodeRules.EnsurePrefix("L01", WcsPackIds.FourWay);
        var layer = await _db.WmsLayers.FirstOrDefaultAsync(x => x.Code == code && x.WarehouseId == warehouseId, ct);
        if (layer != null) return layer.Id;

        var zoneCode = PackCodeRules.EnsurePrefix("Z-SIM", WcsPackIds.FourWay);
        var zone = await _db.WmsZones.FirstOrDefaultAsync(x => x.Code == zoneCode && x.WarehouseId == warehouseId, ct);
        if (zone == null)
        {
            zone = new WmsZone
            {
                WarehouseId = warehouseId,
                PackId = WcsPackIds.FourWay,
                Code = zoneCode,
                Name = "仿真四向库区",
                CreateDate = DateTime.UtcNow
            };
            _db.WmsZones.Add(zone);
            await _db.SaveChangesAsync(ct);
        }

        layer = new WmsLayer
        {
            WarehouseId = warehouseId,
            ZoneId = zone.Id,
            PackId = WcsPackIds.FourWay,
            Code = code,
            Name = "仿真一层",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        _db.WmsLayers.Add(layer);
        await _db.SaveChangesAsync(ct);
        return layer.Id;
    }

    private async Task<int> EnsureStackerSeedAsync(SimProjectDto project, string packId, CancellationToken ct)
    {
        var edgeCount = 0;
        foreach (var edge in project.Map.Edges ?? [])
        {
            var fromCode = PackCodeRules.EnsurePrefix(edge.From.Trim(), packId);
            var toCode = PackCodeRules.EnsurePrefix(edge.To.Trim(), packId);
            var exists = await _db.StkRoutes.AnyAsync(
                x => x.MapCode == string.Empty && x.FromCode == fromCode && x.ToCode == toCode, ct);
            if (exists) continue;
            _db.StkRoutes.Add(new StkRoute
            {
                FromCode = fromCode,
                ToCode = toCode,
                Weight = 1,
                Capacity = 1,
                IsEnabled = true,
                CreateDate = DateTime.UtcNow
            });
            edgeCount++;
        }

        var requestPoints = project.Map.RequestPoints ?? [];
        if (requestPoints.Count > 0)
        {
            foreach (var rp in requestPoints)
            {
                if (string.IsNullOrWhiteSpace(rp.Code)) continue;
                var code = PackCodeRules.EnsurePrefix(rp.Code.Trim(), packId);
                var exists = await _db.StkRequestPoints.AnyAsync(x => x.Code == code, ct);
                if (exists) continue;
                var mapped = string.IsNullOrWhiteSpace(rp.MappedLocationCode)
                    ? code
                    : PackCodeRules.EnsurePrefix(rp.MappedLocationCode.Trim(), packId);
                _db.StkRequestPoints.Add(new StkRequestPoint
                {
                    Code = code,
                    PointType = StkRequestPointType.AisleRequest,
                    IsEnabled = true,
                    AisleCode = mapped,
                    CreateDate = DateTime.UtcNow
                });
            }
        }
        else
        {
            foreach (var node in project.Map.Nodes)
            {
                var raw = node.Code?.Trim();
                if (string.IsNullOrEmpty(raw)) continue;
                var code = PackCodeRules.EnsurePrefix(raw, packId);
                var exists = await _db.StkRequestPoints.AnyAsync(x => x.Code == code, ct);
                if (exists) continue;
                _db.StkRequestPoints.Add(new StkRequestPoint
                {
                    Code = code,
                    PointType = StkRequestPointType.AisleRequest,
                    IsEnabled = true,
                    AisleCode = code,
                    CreateDate = DateTime.UtcNow
                });
            }
        }

        if (!await _db.StkAssignmentPolicies.AnyAsync(ct))
        {
            foreach (var node in project.Map.Nodes.Take(3))
            {
                var raw = node.Code?.Trim();
                if (string.IsNullOrEmpty(raw)) continue;
                var code = PackCodeRules.EnsurePrefix(raw, packId);
                _db.StkAssignmentPolicies.Add(new StkAssignmentPolicy
                {
                    AisleCode = code,
                    IsAvailable = true,
                    MaxHeight = 9,
                    MaxWeight = 9999,
                    DestinationPointCode = code,
                    CreateDate = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return edgeCount;
    }

    private async Task<int> EnsureFourWayMapAsync(SimProjectDto project, string packId, CancellationToken ct)
    {
        var mapCode = $"SIM_{SanitizeCode(project.Meta.Name)}";
        var existingMaps = await _db.FwMapVersions.Where(x => x.Code == mapCode).ToListAsync(ct);
        foreach (var m in existingMaps)
            m.IsActive = false;

        var map = new FwMapVersion
        {
            Code = mapCode,
            Name = $"仿真-{project.Meta.Name}",
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };
        _db.FwMapVersions.Add(map);
        await _db.SaveChangesAsync(ct);

        var nodeIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in project.Map.Nodes)
        {
            var code = PackCodeRules.EnsurePrefix(node.Code.Trim(), packId);
            var fw = new FwNode
            {
                MapVersionId = map.Id,
                Code = code,
                LocationCode = code,
                CreateDate = DateTime.UtcNow
            };
            _db.FwNodes.Add(fw);
            await _db.SaveChangesAsync(ct);
            nodeIds[code] = fw.Id;
        }

        var edgeCount = 0;
        foreach (var edge in project.Map.Edges ?? [])
        {
            var fromCode = PackCodeRules.EnsurePrefix(edge.From, packId);
            var toCode = PackCodeRules.EnsurePrefix(edge.To, packId);
            if (!nodeIds.TryGetValue(fromCode, out var fromId) || !nodeIds.TryGetValue(toCode, out var toId))
                continue;
            _db.FwRoutes.Add(new FwRoute
            {
                MapVersionId = map.Id,
                FromNodeId = fromId,
                ToNodeId = toId,
                FromCode = fromCode,
                ToCode = toCode,
                Weight = 1,
                Capacity = 1,
                CreateDate = DateTime.UtcNow
            });
            edgeCount++;
        }

        await _db.SaveChangesAsync(ct);
        return edgeCount;
    }

    private async Task EnsureScadaAsync(SimProjectDto project, string packId, CancellationToken ct)
    {
        var views = project.Scada?.Views;
        if (views is { Count: > 0 })
            return;

        var viewCode = $"SIM_{SanitizeCode(project.Meta.Name)}";
        var view = await _db.ScdViews.FirstOrDefaultAsync(x => x.Code == viewCode, ct);
        if (view == null)
        {
            var (width, height) = ComputeScadaCanvasSize(project.Map.Nodes);
            view = new ScdView
            {
                Code = viewCode,
                Name = viewCode,
                Width = width,
                Height = height,
                CreateDate = DateTime.UtcNow
            };
            _db.ScdViews.Add(view);
            await _db.SaveChangesAsync(ct);
        }

        foreach (var node in project.Map.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Code)) continue;
            var locationCode = PackCodeRules.EnsurePrefix(node.Code.Trim(), packId);
            var exists = await _db.ScdNodeBinds.AnyAsync(
                x => x.ViewId == view.Id && x.LocationCode == locationCode, ct);
            if (exists) continue;
            _db.ScdNodeBinds.Add(new ScdNodeBind
            {
                ViewId = view.Id,
                LocationCode = locationCode,
                X = node.X,
                Y = node.Y,
                Label = node.Code.Trim(),
                CreateDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private static (int Width, int Height) ComputeScadaCanvasSize(IReadOnlyList<SimMapNodeDto> nodes)
    {
        if (nodes.Count == 0) return (800, 600);
        var maxX = nodes.Max(n => n.X);
        var maxY = nodes.Max(n => n.Y);
        return ((int)Math.Max(800, maxX + 100), (int)Math.Max(600, maxY + 100));
    }

    private static string SanitizeCode(string name)
    {
        var chars = name.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').ToArray();
        var s = new string(chars);
        return string.IsNullOrWhiteSpace(s) ? "DEMO" : s.ToUpperInvariant();
    }

    private static IReadOnlyList<SimPromoteDeviceDto> ValidatePromoteDevices(SimPromoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectName))
            throw new InvalidOperationException("工程名称不能为空");

        var devices = request.Devices ?? [];
        if (devices.Count == 0)
            throw new InvalidOperationException("至少需要一个设备");

        foreach (var device in devices)
        {
            if (string.IsNullOrWhiteSpace(device.Code))
                throw new InvalidOperationException("设备编码不能为空");
            if (IsLoopbackHost(device.Host))
                throw new InvalidOperationException($"禁止环回地址: {device.Host}");
        }

        return devices;
    }

    private async Task<IReadOnlyList<string>> GetPromoteWarningsAsync(string projectName, CancellationToken ct)
    {
        var warnings = new List<string>();
        var deployment = await _db.SimDeployments.AsNoTracking()
            .Where(x => x.ProjectName == projectName.Trim()
                        && (x.Status == "Deployed" || x.Status == "Promoted"))
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (deployment == null)
            warnings.Add($"未找到已部署工程: {projectName.Trim()}");
        else if (deployment.Status == "Promoted")
            warnings.Add("工程已 Promote，将覆盖 CommConnection 与 promote 快照");

        return warnings;
    }

    private async Task<SimDeployment?> FindActiveDeploymentAsync(string projectName, CancellationToken ct)
    {
        return await _db.SimDeployments
            .Where(x => x.ProjectName == projectName.Trim()
                        && (x.Status == "Deployed" || x.Status == "Promoted"))
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<int> UpsertCommConnectionsAsync(
        IReadOnlyList<SimPromoteDeviceDto> devices,
        CancellationToken ct)
    {
        var count = 0;
        foreach (var device in devices)
        {
            var name = device.Code.Trim();
            var conn = await _db.CommConnections.FirstOrDefaultAsync(x => x.Name == name, ct);
            if (conn == null)
            {
                conn = new CommConnection
                {
                    Name = name,
                    CreateDate = DateTime.UtcNow
                };
                _db.CommConnections.Add(conn);
            }

            conn.Host = device.Host.Trim();
            conn.Port = device.Port;
            conn.Protocol = ParseProtocol(device.Protocol);
            conn.Enabled = true;
            conn.AutoConnect = true;
            conn.Remark = "Simulator Promote";
            conn.ModifyDate = DateTime.UtcNow;
            count++;
        }

        return count;
    }

    private static bool IsLoopbackHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        var h = host.Trim();
        return h.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || h.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || h.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static CommProtocol ParseProtocol(string protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol)) return CommProtocol.Step7;
        return protocol.Trim().ToLowerInvariant() switch
        {
            "modbustcp" or "modbus" or "modbus tcp" => CommProtocol.ModbusTcp,
            _ => CommProtocol.Step7
        };
    }
}
