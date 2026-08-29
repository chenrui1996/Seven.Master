namespace Seven.Application.Simulator;

public record SimFeaturesDto(
    bool Wms,
    bool OrchestrationBus,
    bool HotStore,
    bool DeviceComm,
    bool Simulator,
    SimWcsPacksDto WcsPacks);

public record SimWcsPacksDto(bool Stacker, bool FourWay, bool BoxSort);

public record SimMapNodeDto(string Id, string Code, double X, double Y);
public record SimMapEdgeDto(string Id, string From, string To);
public record SimMapDeviceDto(string Id, string Code, string Type, double X, double Y);

public record SimMapDto(
    string PackId,
    IReadOnlyList<SimMapNodeDto> Nodes,
    IReadOnlyList<SimMapEdgeDto>? Edges = null,
    IReadOnlyList<SimMapDeviceDto>? Devices = null);

public record SimProjectDto(
    int Version,
    SimProjectMetaDto Meta,
    SimMapDto Map);

public record SimProjectMetaDto(
    string Name,
    SimFeaturesDto Features,
    string RuntimeMode);

public record ValidateFeaturesResult(bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

public record DeployResult(
    int DeploymentId,
    string WarehouseCode,
    int LocationCount,
    int EdgeCount,
    string Message);

public interface ISimulationDeployService
{
    ValidateFeaturesResult ValidateFeatures(SimFeaturesDto features);
    Task<DeployResult> DeployAsync(SimProjectDto project, CancellationToken ct = default);
    Task UndeployAsync(string projectName, bool removeLocations = false, CancellationToken ct = default);
    Task<IReadOnlyList<SimDeploymentSummary>> ListDeploymentsAsync(CancellationToken ct = default);
}

public record SimDeploymentSummary(
    int Id,
    string ProjectName,
    string PackId,
    string WarehouseCode,
    string Status,
    int LocationCount,
    DateTime? DeployedAt);
