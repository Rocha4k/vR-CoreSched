namespace Warehouse.Backend.Contracts;

public sealed record LightingStateDto(
    string DeviceId,
    string Zone,
    string Name,
    bool IsOn,
    DateTimeOffset Timestamp,
    string Source);
