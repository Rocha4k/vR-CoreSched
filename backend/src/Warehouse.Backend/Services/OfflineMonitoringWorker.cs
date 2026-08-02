using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Hubs;

namespace Warehouse.Backend.Services;

public sealed class OfflineMonitoringWorker : BackgroundService
{
    private readonly IRuleEngine _ruleEngine;
    private readonly IHubContext<OperationsHub> _hub;
    private readonly WarehouseOptions _options;
    private readonly ILogger<OfflineMonitoringWorker> _logger;

    public OfflineMonitoringWorker(
        IRuleEngine ruleEngine,
        IHubContext<OperationsHub> hub,
        IOptions<WarehouseOptions> options,
        ILogger<OfflineMonitoringWorker> logger)
    {
        _ruleEngine = ruleEngine;
        _hub = hub;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.OfflineScanSeconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                foreach (var alert in await _ruleEngine.EvaluateOfflineMachinesAsync(stoppingToken))
                {
                    await _hub.Clients.All.SendAsync("alert.created", alert, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Offline machine detection failed.");
            }
        }
    }
}
