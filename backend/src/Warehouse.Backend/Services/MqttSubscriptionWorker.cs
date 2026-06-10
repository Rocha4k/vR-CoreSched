using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Hubs;
using Warehouse.Backend.Infrastructure;

namespace Warehouse.Backend.Services;

public sealed class MqttSubscriptionWorker : BackgroundService
{
    private readonly IWarehouseStore _store;
    private readonly IRuleEngine _ruleEngine;
    private readonly IHubContext<OperationsHub> _hub;
    private readonly WarehouseOptions _options;

    public MqttSubscriptionWorker(
        IWarehouseStore store,
        IRuleEngine ruleEngine,
        IHubContext<OperationsHub> hub,
        IOptions<WarehouseOptions> options)
    {
        _store = store;
        _ruleEngine = ruleEngine;
        _hub = hub;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += async message =>
        {
            var topic = message.ApplicationMessage.Topic ?? string.Empty;
            var payload = message.ApplicationMessage.PayloadSegment.ToArray();
            var json = System.Text.Encoding.UTF8.GetString(payload);

            if (topic.Contains("telemetry", StringComparison.OrdinalIgnoreCase))
            {
                var telemetry = JsonSerializer.Deserialize<MachineTelemetryDto>(json, JsonOptions);
                if (telemetry is not null)
                {
                    await _store.UpsertTelemetryAsync(telemetry, stoppingToken);
                    var alert = await _ruleEngine.EvaluateTelemetryAsync(telemetry, stoppingToken);
                    await _hub.Clients.All.SendAsync("telemetry.received", telemetry, stoppingToken);
                    if (alert is not null)
                    {
                        await _hub.Clients.All.SendAsync("alert.created", alert, stoppingToken);
                    }
                }
            }
            else if (topic.Contains("state", StringComparison.OrdinalIgnoreCase))
            {
                var lighting = JsonSerializer.Deserialize<LightingStateDto>(json, JsonOptions);
                if (lighting is not null)
                {
                    var updated = await _store.UpsertLightingStateAsync(lighting, stoppingToken);
                    if (updated is not null)
                    {
                        await _hub.Clients.All.SendAsync("lighting.updated", updated, stoppingToken);
                    }
                }
            }
        };

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithCleanSession()
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(mqttOptions, stoppingToken);
                    var machineSubscription = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(MqttTopicCatalog.MachineTelemetry).Build();
                    var lightingSubscription = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(MqttTopicCatalog.LightingState).Build();
                    await client.SubscribeAsync(machineSubscription, stoppingToken);
                    await client.SubscribeAsync(lightingSubscription, stoppingToken);
                }
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
