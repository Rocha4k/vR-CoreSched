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
                // A one-off failure must not kill the worker: the next cycle retries.
                _logger.LogError(exception, "Consumption aggregation failed.");
            }
        }
    }

    private async Task AggregateAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        // Aggregates the most recently closed window. The previous version always summed
        // the last 5000 events, counting the same telemetry on every run.
        var windowEnd = Floor(DateTimeOffset.UtcNow, interval);
        var windowStart = windowEnd - interval;

        var written = await _store.WriteConsumptionAggregatesAsync(windowStart, windowEnd, _options.EnergyEuroPerKwh, cancellationToken);
        if (written > 0)
        {
            _logger.LogInformation("Wrote {Count} consumption aggregates for window {Start:o} - {End:o}.", written, windowStart, windowEnd);
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
            _logger.LogInformation("Pruned {Count} telemetry events older than {Cutoff:o}.", removed, cutoff);
        }
    }

    private static DateTimeOffset Floor(DateTimeOffset value, TimeSpan interval)
    {
        var ticks = value.UtcTicks - (value.UtcTicks % interval.Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
