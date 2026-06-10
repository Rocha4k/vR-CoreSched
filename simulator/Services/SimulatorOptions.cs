namespace Warehouse.Simulator.Services;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public string MqttHost { get; set; } = "localhost";
    public int MqttPort { get; set; } = 1883;
    public int PublishIntervalSeconds { get; set; } = 1;
    public int MachineCount { get; set; } = 3;
    public int LightingCount { get; set; } = 4;
}
