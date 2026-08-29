using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Simulator;
using Seven.Domain.Entities.Simulator;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
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
            errors.Add("Deploy 需要 Features.Wms=true（库位权威在 Wms_Location）");

        if (!features.WcsPacks.Stacker && !features.WcsPacks.FourWay && !features.WcsPacks.BoxSort)
            warnings.Add("未启用任何 WCS 包：仅部署库位主数据");

        if (features.WcsPacks.FourWay && !features.HotStore)
            warnings.Add("四向车建议同时开启 HotStore");

        if (features.OrchestrationBus && !features.Wms)
            errors.Add("OrchestrationBus 需要 Wms=true");

        if (!features.Simulator)
            warnings.Add("工程 meta 未勾选 Simulator；API 仍受服务器 Features.Simulator 门控");

        return new ValidateFeaturesResult(errors.Count == 0, errors, warnings);
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

        var packId = string.IsNullOrWhiteSpace(project.Map.PackId) ? "stacker" : project.Map.PackId.Trim().ToLowerInvariant();
        var warehouseCode = SimWarehousePrefix + SanitizeCode(project.Meta.Name);

        var warehouse = await _db.WmsWarehouses.FirstOrDefaultAsync(x => x.Code == warehouseCode, ct);
        if (warehouse == null)
        {
            warehouse = new WmsWarehouse
            {
                Code = warehouseCode,
                Name = $"仿真仓-{project.Meta.Name}",
                CreateDate = DateTime.UtcNow
            };
            _db.WmsWarehouses.Add(warehouse);
            await _db.SaveChangesAsync(ct);
        }

        var locationCount = 0;
        foreach (var node in project.Map.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Code)) continue;
            var code = node.Code.Trim();
            var loc = await _db.WmsLocations.FirstOrDefaultAsync(x => x.Code == code, ct);
            if (loc == null)
            {
                loc = new WmsLocation
                {
                    WarehouseId = warehouse.Id,
                    Code = code,
                    CreateDate = DateTime.UtcNow
                };
                _db.WmsLocations.Add(loc);
                locationCount++;
            }
            else if (loc.WarehouseId != warehouse.Id)
            {
                // 已存在于其他仓：不改归属，仅计数为已就绪
            }
        }

        await _db.SaveChangesAsync(ct);

        var edgeCount = 0;
        if (packId is "stacker")
            await EnsureStackerSeedAsync(project, ct);
        else if (packId is "fourway")
            edgeCount = await EnsureFourWayMapAsync(project, ct);

        // 同名工程若已 Deployed，先标记 Undeployed（不删库位）
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
                if (project?.Map.Nodes != null)
                {
                    foreach (var node in project.Map.Nodes)
                    {
                        var code = node.Code?.Trim();
                        if (string.IsNullOrEmpty(code)) continue;
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

    public async Task<IReadOnlyList<SimDeploymentSummary>> ListDeploymentsAsync(CancellationToken ct = default)
    {
        return await _db.SimDeployments.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(50)
            .Select(x => new SimDeploymentSummary(
                x.Id, x.ProjectName, x.PackId, x.WarehouseCode, x.Status, x.LocationCount, x.DeployedAt))
            .ToListAsync(ct);
    }

    private async Task EnsureStackerSeedAsync(SimProjectDto project, CancellationToken ct)
    {
        // 若工程未带申请点，为每个节点补一个 AisleRequest 入口（Code=节点码），便于仿真 SUDR
        foreach (var node in project.Map.Nodes)
        {
            var code = node.Code?.Trim();
            if (string.IsNullOrEmpty(code)) continue;
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

        if (!await _db.StkAssignmentPolicies.AnyAsync(ct))
        {
            foreach (var node in project.Map.Nodes.Take(3))
            {
                var code = node.Code?.Trim();
                if (string.IsNullOrEmpty(code)) continue;
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
    }

    private async Task<int> EnsureFourWayMapAsync(SimProjectDto project, CancellationToken ct)
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
            var code = node.Code.Trim();
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
            if (!nodeIds.TryGetValue(edge.From, out var fromId) || !nodeIds.TryGetValue(edge.To, out var toId))
                continue;
            _db.FwRoutes.Add(new FwRoute
            {
                MapVersionId = map.Id,
                FromNodeId = fromId,
                ToNodeId = toId,
                FromCode = edge.From,
                ToCode = edge.To,
                Weight = 1,
                Capacity = 1,
                CreateDate = DateTime.UtcNow
            });
            edgeCount++;
        }

        await _db.SaveChangesAsync(ct);
        return edgeCount;
    }

    private static string SanitizeCode(string name)
    {
        var chars = name.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').ToArray();
        var s = new string(chars);
        return string.IsNullOrWhiteSpace(s) ? "DEMO" : s.ToUpperInvariant();
    }
}
