namespace Warehouse.Simulator.Models;

public sealed record MachineTelemetry(
    string MachineId,
    string Name,
    string Zone,
    DateTimeOffset Timestamp,
    decimal TemperatureC,
    decimal VibrationMs2,
    int Rpm,
    decimal EnergyKwh,
    string Source);
