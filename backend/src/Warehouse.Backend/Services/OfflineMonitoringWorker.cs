using Microsoft.AspNetCore.SignalR;
using Warehouse.Backend.Hubs;

namespace Warehouse.Backend.Services;

public sealed class OfflineMonitoringWorker : BackgroundService
{
    private readonly IRuleEngine _ruleEngine;
    private readonly IHubContext<OperationsHub> _hub;

    public OfflineMonitoringWorker(IRuleEngine ruleEngine, IHubContext<OperationsHub> hub)
    {
        _ruleEngine = ruleEngine;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var alerts = await _ruleEngine.EvaluateOfflineMachinesAsync(stoppingToken);
            foreach (var alert in alerts)
            {
                await _hub.Clients.All.SendAsync("alert.created", alert, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
