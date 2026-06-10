namespace Warehouse.Backend.Contracts;

public sealed record MachineStateDto(
    string MachineId,
    string Name,
    string Zone,
    bool IsOnline,
    DateTimeOffset LastSeen,
    decimal TemperatureC,
    decimal VibrationMs2,
    int Rpm,
    decimal EnergyKwh,
    string Severity);
