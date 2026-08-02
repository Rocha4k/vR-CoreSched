using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Warehouse.Backend.Data;
using Warehouse.Backend.Services;

namespace Warehouse.Backend.Infrastructure;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<WarehouseDbContext> _dbContextFactory;

    public DatabaseHealthCheck(IDbContextFactory<WarehouseDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Failed to reach PostgreSQL.", exception);
        }
    }
}

public sealed class MqttHealthCheck : IHealthCheck
{
    private readonly MqttSubscriptionWorker _worker;

    public MqttHealthCheck(MqttSubscriptionWorker worker)
    {
        _worker = worker;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Degraded rather than Unhealthy: the API still serves historical data without a broker.
        return Task.FromResult(_worker.IsConnected
            ? HealthCheckResult.Healthy("MQTT broker connected.")
            : HealthCheckResult.Degraded("No connection to the MQTT broker."));
    }
}
