using Warehouse.Backend.Contracts;
using Warehouse.Backend.Infrastructure;

namespace Warehouse.Backend.Services;

public sealed class ConsumptionAggregationWorker : BackgroundService
{
    private readonly IWarehouseStore _store;
    private readonly WarehouseOptions _options;

    public ConsumptionAggregationWorker(IWarehouseStore store, IConfiguration configuration)
    {
        _store = store;
        _options = configuration.GetSection(WarehouseOptions.SectionName).Get<WarehouseOptions>() ?? new WarehouseOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.AggregationIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            await AggregateAsync(stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task AggregateAsync(CancellationToken cancellationToken)
    {
        var telemetry = await _store.GetAllRecentTelemetryAsync(cancellationToken);
        var groupStart = DateTimeOffset.UtcNow.AddMinutes(-_options.AggregationIntervalMinutes);

        foreach (var group in telemetry.GroupBy(item => item.MachineId))
        {
            var samples = group.ToList();
            if (samples.Count == 0)
            {
                continue;
            }

            var total = samples.Sum(item => item.EnergyKwh);
            var average = samples.Average(item => item.EnergyKwh);
            var aggregate = new ConsumptionAggregateDto(
                Guid.NewGuid().ToString("N"),
                "Machine",
                group.Key,
                groupStart,
                DateTimeOffset.UtcNow,
                (decimal)average,
                total,
                total * _options.EnergyEuroPerKwh);

            await _store.AddAggregateAsync(aggregate, cancellationToken);
        }
    }
}
