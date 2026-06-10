namespace Warehouse.Backend.Contracts;

public sealed record AdminMachineDto(
    string MachineId,
    string Name,
    string ZoneId,
    bool IsEnabled,
    bool IsOnline,
    DateTimeOffset? LastSeen,
    decimal TemperatureC,
    decimal VibrationMs2,
    int Rpm,
    decimal EnergyKwh,
    string Severity,
    decimal LocationX,
    decimal LocationY);
