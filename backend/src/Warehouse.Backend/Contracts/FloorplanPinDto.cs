namespace Warehouse.Backend.Contracts;

public sealed record FloorplanPinDto(
    int Id,
    string DeviceType,
    string DeviceId,
    string Label,
    decimal X,
    decimal Y,
    bool IsVisible,
    string ZoneId);
