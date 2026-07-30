using Microsoft.Extensions.Options;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Infrastructure;

namespace Warehouse.Backend.Services;

public sealed class ConsumptionAggregationWorker : BackgroundService
{
    private readonly IWarehouseStore _store;
    private readonly WarehouseOptions _options;
    private readonly ILogger<ConsumptionAggregationWorker> _logger;

    public ConsumptionAggregationWorker(IWarehouseStore store, IOptions<WarehouseOptions> options, ILogger<ConsumptionAggregationWorker> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.AggregationIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await AggregateAsync(interval, stoppingToken);
                await PurgeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Uma falha pontual não pode matar o worker: o ciclo seguinte volta a tentar.
                _logger.LogError(exception, "Falha na agregação de consumo.");
            }
        }
    }

    private async Task AggregateAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        // Agrega a janela fechada mais recente. A versão anterior somava sempre os
        // últimos 5000 eventos, contando a mesma telemetria em cada execução.
        var windowEnd = Floor(DateTimeOffset.UtcNow, interval);
        var windowStart = windowEnd - interval;

        var written = await _store.WriteConsumptionAggregatesAsync(windowStart, windowEnd, _options.EnergyEuroPerKwh, cancellationToken);
        if (written > 0)
        {
            _logger.LogInformation("Agregados {Count} registos de consumo para a janela {Start:o} - {End:o}.", written, windowStart, windowEnd);
        }
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        if (_options.TelemetryRetentionDays <= 0)
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.TelemetryRetentionDays);
        var removed = await _store.PurgeTelemetryOlderThanAsync(cutoff, cancellationToken);
        if (removed > 0)
        {
            _logger.LogInformation("Removidos {Count} eventos de telemetria anteriores a {Cutoff:o}.", removed, cutoff);
        }
    }

    private static DateTimeOffset Floor(DateTimeOffset value, TimeSpan interval)
    {
        var ticks = value.UtcTicks - (value.UtcTicks % interval.Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
