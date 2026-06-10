namespace Warehouse.Backend.Contracts;

public sealed record AdminZoneDto(
    string ZoneId,
    string Name,
    string Description,
    string Color,
    bool IsActive);
