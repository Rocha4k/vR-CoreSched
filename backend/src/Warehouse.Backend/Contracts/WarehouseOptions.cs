namespace Warehouse.Backend.Contracts;

public sealed class WarehouseOptions
{
    public const string SectionName = "Warehouse";

    public string MqttHost { get; set; } = "localhost";
    public int MqttPort { get; set; } = 1883;
    public int AggregationIntervalMinutes { get; set; } = 60;
    public int AlertCooldownSeconds { get; set; } = 30;
    public decimal EnergyEuroPerKwh { get; set; } = 0.18m;
}
