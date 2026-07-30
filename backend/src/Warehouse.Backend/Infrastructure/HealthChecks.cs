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
                ? HealthCheckResult.Healthy("PostgreSQL acessível.")
                : HealthCheckResult.Unhealthy("PostgreSQL inacessível.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Falha a contactar o PostgreSQL.", exception);
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
        // Degraded e não Unhealthy: a API continua a servir dados históricos sem broker.
        return Task.FromResult(_worker.IsConnected
            ? HealthCheckResult.Healthy("Broker MQTT ligado.")
            : HealthCheckResult.Degraded("Sem ligação ao broker MQTT."));
    }
}
