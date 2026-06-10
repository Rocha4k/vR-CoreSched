namespace Warehouse.Simulator.Models;

public sealed record LightingState(
    string DeviceId,
    string Zone,
    string Name,
    bool IsOn,
    DateTimeOffset Timestamp,
    string Source);
