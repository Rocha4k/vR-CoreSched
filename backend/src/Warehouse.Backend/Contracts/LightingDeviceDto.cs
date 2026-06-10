namespace Warehouse.Backend.Contracts;

public sealed record LightingDeviceDto(
    string Id,
    string Zone,
    string Name,
    bool IsOn,
    DateTimeOffset LastChangedAt,
    string LastCommandSource);
