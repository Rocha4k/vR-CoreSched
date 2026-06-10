namespace Warehouse.Backend.Contracts;

public sealed record FloorplanLayoutDto(
    int Id,
    string Name,
    int CanvasWidth,
    int CanvasHeight,
    string TextureKey,
    string BoundaryPointsJson,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FloorplanPinDto> Pins);
