using Warehouse.Backend.Contracts;

namespace Warehouse.Backend.Services;

public interface IRuleEngine
{
    Task<AlertDto?> EvaluateTelemetryAsync(MachineTelemetryDto telemetry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertDto>> EvaluateOfflineMachinesAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops the rule cache after an administrative change.</summary>
    void InvalidateRules();
}
