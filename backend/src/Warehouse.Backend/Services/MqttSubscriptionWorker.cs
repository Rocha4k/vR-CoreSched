using System.Text;
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
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly IWarehouseStore _store;
    private readonly IRuleEngine _ruleEngine;
    private readonly IHubContext<OperationsHub> _hub;
    private readonly WarehouseOptions _options;
    private readonly ILogger<MqttSubscriptionWorker> _logger;
    private IMqttClient? _client;

    public MqttSubscriptionWorker(
        IWarehouseStore store,
        IRuleEngine ruleEngine,
        IHubContext<OperationsHub> hub,
        IOptions<WarehouseOptions> options,
        ILogger<MqttSubscriptionWorker> logger)
    {
        _store = store;
        _ruleEngine = ruleEngine;
        _hub = hub;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Consumido pelo health check de readiness.</summary>
    public bool IsConnected => _client?.IsConnected ?? false;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new MqttFactory().CreateMqttClient();
        _client = client;

        client.ApplicationMessageReceivedAsync += message => HandleMessageAsync(message, stoppingToken);
        client.ConnectedAsync += async _ =>
        {
            // Subscrever no evento de ligação garante que uma reconexão volta a subscrever.
            await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(MqttTopicCatalog.MachineTelemetry)
                .WithTopicFilter(MqttTopicCatalog.LightingState)
                .Build(), stoppingToken);

            _logger.LogInformation("Ligado ao broker MQTT {Host}:{Port}.", _options.MqttHost, _options.MqttPort);
        };
        client.DisconnectedAsync += _ =>
        {
            _logger.LogWarning("Ligação MQTT perdida. A tentar reconectar.");
            return Task.CompletedTask;
        };

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithClientId($"vrcoresched-backend-{Environment.MachineName}")
            .WithCleanSession()
            .Build();

        var backoff = TimeSpan.FromSeconds(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(mqttOptions, stoppingToken);
                    backoff = TimeSpan.FromSeconds(1);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Falha a ligar ao broker MQTT. Nova tentativa em {Delay}.", backoff);
                await Task.Delay(backoff, stoppingToken);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }

        if (client.IsConnected)
        {
            await client.DisconnectAsync(cancellationToken: CancellationToken.None);
        }
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs message, CancellationToken cancellationToken)
    {
        var topic = message.ApplicationMessage.Topic ?? string.Empty;

        try
        {
            var json = Encoding.UTF8.GetString(message.ApplicationMessage.PayloadSegment.AsSpan());

            switch (MqttTopicCatalog.Classify(topic))
            {
                case MqttTopicKind.MachineTelemetry:
                    await HandleTelemetryAsync(json, cancellationToken);
                    break;
                case MqttTopicKind.LightingState:
                    await HandleLightingAsync(json, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Encerramento normal.
        }
        catch (Exception exception)
        {
            // Uma mensagem inválida não pode derrubar o consumidor MQTT.
            _logger.LogError(exception, "Falha a processar mensagem MQTT do tópico {Topic}.", topic);
        }
    }

    private async Task HandleTelemetryAsync(string json, CancellationToken cancellationToken)
    {
        var telemetry = JsonSerializer.Deserialize<MachineTelemetryDto>(json, JsonOptions);
        if (telemetry is null || string.IsNullOrWhiteSpace(telemetry.MachineId) || telemetry.Timestamp == default)
        {
            _logger.LogDebug("Telemetria descartada por payload inválido.");
            return;
        }

        await _store.UpsertTelemetryAsync(telemetry, cancellationToken);
        var alert = await _ruleEngine.EvaluateTelemetryAsync(telemetry, cancellationToken);

        await _hub.Clients.All.SendAsync("telemetry.received", telemetry, cancellationToken);
        if (alert is not null)
        {
            await _hub.Clients.All.SendAsync("alert.created", alert, cancellationToken);
        }
    }

    private async Task HandleLightingAsync(string json, CancellationToken cancellationToken)
    {
        var lighting = JsonSerializer.Deserialize<LightingStateDto>(json, JsonOptions);
        if (lighting is null || string.IsNullOrWhiteSpace(lighting.DeviceId))
        {
            _logger.LogDebug("Estado de iluminação descartado por payload inválido.");
            return;
        }

        var updated = await _store.UpsertLightingStateAsync(lighting, cancellationToken);
        if (updated is not null)
        {
            await _hub.Clients.All.SendAsync("lighting.updated", updated, cancellationToken);
        }
    }

    public override void Dispose()
    {
        _client?.Dispose();
        base.Dispose();
    }
}
