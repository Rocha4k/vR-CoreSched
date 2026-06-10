using System.Text.Json;

namespace Warehouse.Backend.Data;

public sealed class MachineEntity
{
    public string MachineId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastSeen { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal VibrationMs2 { get; set; }
    public int Rpm { get; set; }
    public decimal EnergyKwh { get; set; }
    public string Severity { get; set; } = "Info";
    public decimal LocationX { get; set; }
    public decimal LocationY { get; set; }
}

public sealed class LightingDeviceEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public DateTimeOffset LastChangedAt { get; set; }
    public string LastCommandSource { get; set; } = string.Empty;
    public decimal LocationX { get; set; }
    public decimal LocationY { get; set; }
    public bool IsVisible { get; set; } = true;
}

public sealed class TelemetryEventEntity
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal VibrationMs2 { get; set; }
    public int Rpm { get; set; }
    public decimal EnergyKwh { get; set; }
    public string Source { get; set; } = string.Empty;
}

public sealed class AlertEntity
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string RuleCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? AcknowledgementNote { get; set; }
}

public sealed class ConsumptionAggregateEntity
{
    public Guid Id { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeId { get; set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public decimal AverageKwh { get; set; }
    public decimal TotalKwh { get; set; }
    public decimal CostEuro { get; set; }
}

public sealed class RuleDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public decimal TemperatureThreshold { get; set; }
    public decimal VibrationThreshold { get; set; }
    public int DurationSeconds { get; set; }
    public int CooldownSeconds { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class ZoneEntity
{
    public string ZoneId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class FloorplanLayoutEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    public string TextureKey { get; set; } = string.Empty;
    public string BoundaryPointsJson { get; set; } = "[]";
    public DateTimeOffset UpdatedAt { get; set; }
    public List<FloorplanPinEntity> Pins { get; set; } = [];

    public IReadOnlyList<FloorplanPoint> BoundaryPoints
    {
        get => JsonSerializer.Deserialize<List<FloorplanPoint>>(BoundaryPointsJson) ?? [];
        set => BoundaryPointsJson = JsonSerializer.Serialize(value);
    }
}

public sealed class FloorplanPinEntity
{
    public int Id { get; set; }
    public int FloorplanLayoutId { get; set; }
    public FloorplanLayoutEntity? FloorplanLayout { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public bool IsVisible { get; set; } = true;
    public string ZoneId { get; set; } = string.Empty;
}

public sealed class MaintenanceRecordEntity
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public string? AlertId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
}

public sealed record FloorplanPoint(decimal X, decimal Y);
