namespace Warehouse.Backend.Contracts;

public sealed record MachineTelemetryDto(
    string MachineId,
    string Name,
    string Zone,
    DateTimeOffset Timestamp,
    decimal TemperatureC,
    decimal VibrationMs2,
    int Rpm,
    decimal EnergyKwh,
    string Source);
