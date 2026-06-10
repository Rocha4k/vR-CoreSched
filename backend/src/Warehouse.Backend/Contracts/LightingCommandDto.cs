namespace Warehouse.Backend.Contracts;

public sealed record LightingCommandDto(bool IsOn, string Source, DateTimeOffset Timestamp);
