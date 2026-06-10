namespace Warehouse.Backend.Services;

public static class MqttTopicCatalog
{
    public const string MachineTelemetry = "warehouse/machines/+/telemetry";
    public const string LightingState = "warehouse/lighting/+/state";
}
