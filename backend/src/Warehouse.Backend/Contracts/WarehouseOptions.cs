namespace Warehouse.Backend.Contracts;

public sealed class WarehouseOptions
{
    public const string SectionName = "Warehouse";

    public string MqttHost { get; set; } = "localhost";
    public int MqttPort { get; set; } = 1883;
    public int AggregationIntervalMinutes { get; set; } = 60;
    public int AlertCooldownSeconds { get; set; } = 30;
    public decimal EnergyEuroPerKwh { get; set; } = 0.18m;

    /// <summary>Seconds without telemetry after which a machine counts as offline.</summary>
    public int OfflineThresholdSeconds { get; set; } = 10;

    /// <summary>Interval between offline-detection sweeps.</summary>
    public int OfflineScanSeconds { get; set; } = 5;

    /// <summary>Lifetime of the rule cache used on the telemetry hot path.</summary>
    public int RuleCacheSeconds { get; set; } = 30;

    /// <summary>Raw telemetry retention in days. 0 disables pruning.</summary>
    public int TelemetryRetentionDays { get; set; } = 7;

    /// <summary>Caps applied to the lists returned by the dashboard snapshot.</summary>
    public int SnapshotAlertLimit { get; set; } = 100;
    public int SnapshotAggregateLimit { get; set; } = 200;
    public int SnapshotMaintenanceLimit { get; set; } = 100;

    /// <summary>Allowed CORS origins.</summary>
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}
