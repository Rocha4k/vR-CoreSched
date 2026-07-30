using Warehouse.Backend.Contracts;

namespace Warehouse.Backend.Services;

public interface IRuleEngine
{
    Task<AlertDto?> EvaluateTelemetryAsync(MachineTelemetryDto telemetry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertDto>> EvaluateOfflineMachinesAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta a cache de regras após uma alteração administrativa.</summary>
    void InvalidateRules();
}
