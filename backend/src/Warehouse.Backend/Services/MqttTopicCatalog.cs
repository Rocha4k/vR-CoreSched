namespace Warehouse.Backend.Services;

public static class MqttTopicCatalog
{
    public const string MachineTelemetry = "warehouse/machines/+/telemetry";
    public const string LightingState = "warehouse/lighting/+/state";

    private const string MachinePrefix = "warehouse/machines/";
    private const string LightingPrefix = "warehouse/lighting/";

    /// <summary>
    /// Classifies the topic by prefix and suffix. The previous <c>Contains("state")</c>
    /// match also caught warehouse/machines/{id}/state.
    /// </summary>
    public static MqttTopicKind Classify(string topic) => topic switch
    {
        _ when topic.StartsWith(MachinePrefix, StringComparison.Ordinal) && topic.EndsWith("/telemetry", StringComparison.Ordinal) => MqttTopicKind.MachineTelemetry,
        _ when topic.StartsWith(LightingPrefix, StringComparison.Ordinal) && topic.EndsWith("/state", StringComparison.Ordinal) => MqttTopicKind.LightingState,
        _ => MqttTopicKind.Unknown
    };
}

public enum MqttTopicKind
{
    Unknown,
    MachineTelemetry,
    LightingState
}
