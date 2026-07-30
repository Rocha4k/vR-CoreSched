namespace Warehouse.Backend.Contracts;

public sealed class WarehouseOptions
{
    public const string SectionName = "Warehouse";

    public string MqttHost { get; set; } = "localhost";
    public int MqttPort { get; set; } = 1883;
    public int AggregationIntervalMinutes { get; set; } = 60;
    public int AlertCooldownSeconds { get; set; } = 30;
    public decimal EnergyEuroPerKwh { get; set; } = 0.18m;

    /// <summary>Segundos sem telemetria a partir dos quais a máquina é considerada offline.</summary>
    public int OfflineThresholdSeconds { get; set; } = 10;

    /// <summary>Intervalo entre varrimentos de deteção de máquinas offline.</summary>
    public int OfflineScanSeconds { get; set; } = 5;

    /// <summary>Tempo de vida da cache de regras usada no caminho quente da telemetria.</summary>
    public int RuleCacheSeconds { get; set; } = 30;

    /// <summary>Dias de retenção da telemetria bruta. 0 desliga a limpeza.</summary>
    public int TelemetryRetentionDays { get; set; } = 7;

    /// <summary>Limites aplicados às listas devolvidas pelo snapshot do dashboard.</summary>
    public int SnapshotAlertLimit { get; set; } = 100;
    public int SnapshotAggregateLimit { get; set; } = 200;
    public int SnapshotMaintenanceLimit { get; set; } = 100;

    /// <summary>Origens autorizadas para CORS.</summary>
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}
