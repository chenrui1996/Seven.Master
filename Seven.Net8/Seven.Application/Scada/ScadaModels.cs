namespace Seven.Application.Scada;

public record ScdNodeStatusDto(
    int BindId,
    string LocationCode,
    double X,
    double Y,
    string? Label,
    bool IsOccupied);

public record ScdViewStatusDto(
    int ViewId,
    string Code,
    string Name,
    int Width,
    int Height,
    IReadOnlyList<ScdNodeStatusDto> Nodes);
