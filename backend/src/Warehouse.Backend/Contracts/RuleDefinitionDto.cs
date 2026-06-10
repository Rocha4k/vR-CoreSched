namespace Warehouse.Backend.Contracts;

public sealed record RuleDefinitionDto(
    string Id,
    string Code,
    string Name,
    string TargetType,
    string? TargetId,
    string Severity,
    decimal TemperatureThreshold,
    decimal VibrationThreshold,
    int DurationSeconds,
    int CooldownSeconds,
    bool IsEnabled);
