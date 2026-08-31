namespace Seven.Application.Simulator;

public record ImportGridCellDto(string Code, double X, double Y);

public record ImportGridRequest(string PackId, IReadOnlyList<ImportGridCellDto> Cells);

public record ImportGridResult(SimMapDto Map, IReadOnlyList<string> Warnings);

public record RouteGroupsPreviewRequest(
    string PackId,
    IReadOnlyList<SimMapNodeDto> Nodes,
    IReadOnlyList<SimMapEdgeDto>? Edges = null);

public record RouteGroupSuggestionDto(string Code, IReadOnlyList<string> NodeIds);

public record RouteGroupsPreviewResult(
    IReadOnlyList<RouteGroupSuggestionDto> Suggested,
    IReadOnlyList<string> Warnings);

public interface ISimulationImportService
{
    ImportGridResult ImportGrid(ImportGridRequest request);
    RouteGroupsPreviewResult PreviewRouteGroups(RouteGroupsPreviewRequest request);
}
