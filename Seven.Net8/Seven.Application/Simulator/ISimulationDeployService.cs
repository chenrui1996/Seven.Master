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
public record SimMapRequestPointDto(string Code, string MappedLocationCode);

public record SimMapDto(
    string PackId,
    IReadOnlyList<SimMapNodeDto> Nodes,
    IReadOnlyList<SimMapEdgeDto>? Edges = null,
    IReadOnlyList<SimMapDeviceDto>? Devices = null,
    IReadOnlyList<SimMapRequestPointDto>? RequestPoints = null);

public record SimScadaViewDto(string Code, string Name, int Width, int Height);
public record SimScadaDto(IReadOnlyList<SimScadaViewDto>? Views = null);

public record SimPromoteDeviceDto(string Code, string Host, int Port, string Protocol);
public record SimPromoteDto(IReadOnlyList<SimPromoteDeviceDto>? Devices = null);

public record SimProjectDto(
    int Version,
    SimProjectMetaDto Meta,
    SimMapDto Map,
    SimScadaDto? Scada = null,
    SimPromoteDto? Promote = null);

public record SimProjectMetaDto(
    string Name,
    SimFeaturesDto Features,
    string RuntimeMode,
    string SimCommsMode = "Trigger");

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
    Task ResetAsync(string projectName, CancellationToken ct = default);
    Task<SimPromotePreviewResult> PromotePreviewAsync(SimPromoteRequest request, CancellationToken ct = default);
    Task<SimPromoteResult> PromoteAsync(SimPromoteRequest request, CancellationToken ct = default);
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

public record SimPromoteRequest(
    string ProjectName,
    IReadOnlyList<SimPromoteDeviceDto> Devices);

public record SimPromotePreviewResult(
    string ProjectName,
    IReadOnlyList<SimPromoteDeviceDto> Devices,
    IReadOnlyList<string> Warnings);

public record SimPromoteResult(
    int DeploymentId,
    string ProjectName,
    string WarehouseCode,
    string Status,
    int CommConnectionCount,
    string Message);
