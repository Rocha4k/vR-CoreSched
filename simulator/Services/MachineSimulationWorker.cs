using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Warehouse.Simulator.Models;

namespace Warehouse.Simulator.Services;

public sealed class MachineSimulationWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    // Chance, per cycle, that a lamp flips state on its own.
    private const int LightingChangeChancePercent = 2;

    private readonly SimulatorOptions _options;
    private readonly ILogger<MachineSimulationWorker> _logger;
    private readonly Random _random = new();
    private readonly IReadOnlyList<MachineProfile> _machines;
    private readonly List<LightingState> _lighting;
    private bool _lightingPublished;

    public MachineSimulationWorker(IOptions<SimulatorOptions> options, ILogger<MachineSimulationWorker> logger)
    {
        _options = options.Value;
        _logger = logger;

        MachineProfile[] machineCatalog =
        [
            new("press-01", "Hydraulic Press", "production-area"),
            new("line-01", "Assembly Line", "assembly-line"),
            new("belt-01", "Conveyor Belt", "aisle-a")
        ];

        LightingState[] lightingCatalog =
        [
            new("light-loading", "loading-bay", "Loading Bay Light", true, DateTimeOffset.UtcNow, "seed"),
            new("light-aisle-a", "aisle-a", "Aisle A Light", true, DateTimeOffset.UtcNow, "seed"),
            new("light-aisle-b", "aisle-b", "Aisle B Light", true, DateTimeOffset.UtcNow, "seed"),
            new("light-office", "offices", "Office Light", true, DateTimeOffset.UtcNow, "seed")
        ];

        // MachineCount and LightingCount were being ignored by the configuration.
        _machines = machineCatalog.Take(Math.Clamp(_options.MachineCount, 1, machineCatalog.Length)).ToArray();
        _lighting = lightingCatalog.Take(Math.Clamp(_options.LightingCount, 0, lightingCatalog.Length)).ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new MqttFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithClientId($"vrcoresched-simulator-{Environment.MachineName}")
            .WithCleanSession()
            .Build();

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PublishIntervalSeconds));
        var backoff = TimeSpan.FromSeconds(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(options, stoppingToken);
                    _logger.LogInformation("Simulator connected to MQTT broker {Host}:{Port}.", _options.MqttHost, _options.MqttPort);
                    backoff = TimeSpan.FromSeconds(1);
                }

                await PublishTelemetryAsync(client, stoppingToken);
                await PublishLightingAsync(client, stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // The previous empty catch hid broker connection failures.
                _logger.LogWarning(exception, "Failed to publish to the MQTT broker. Retrying in {Delay}.", backoff);
                await Task.Delay(backoff, stoppingToken);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }
    }

    private async Task PublishTelemetryAsync(IMqttClient client, CancellationToken cancellationToken)
    {
        foreach (var machine in _machines)
        {
            var telemetry = new MachineTelemetry(
                machine.MachineId,
                machine.Name,
                machine.Zone,
                DateTimeOffset.UtcNow,
                GenerateTemperature(machine.MachineId),
                GenerateVibration(machine.MachineId),
                GenerateRpm(machine.MachineId),
                GenerateEnergy(machine.MachineId),
                "simulator");

            await PublishAsync(client, $"warehouse/machines/{machine.MachineId}/telemetry", telemetry, cancellationToken);
        }
    }

    private async Task PublishLightingAsync(IMqttClient client, CancellationToken cancellationToken)
    {
        // Re-publishing a random state every second undid operator commands from
        // the UI. Now a message only goes out when the simulated state changes.
        for (var index = 0; index < _lighting.Count; index++)
        {
            var current = _lighting[index];
            var toggles = _random.Next(0, 100) < LightingChangeChancePercent;
            if (!toggles && _lightingPublished)
            {
                continue;
            }

            var next = current with
            {
                IsOn = toggles ? !current.IsOn : current.IsOn,
                Timestamp = DateTimeOffset.UtcNow,
                Source = "simulator"
            };

            _lighting[index] = next;
            await PublishAsync(client, $"warehouse/lighting/{next.DeviceId}/state", next, cancellationToken);
        }

        _lightingPublished = true;
    }

    private static Task PublishAsync<T>(IMqttClient client, string topic, T payload, CancellationToken cancellationToken)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        return client.PublishAsync(message, cancellationToken);
    }

    private decimal GenerateTemperature(string key)
    {
        var baseValue = key == "press-01" ? 76 : 62;
        return (decimal)(baseValue + _random.NextDouble() * 18);
    }

    private decimal GenerateVibration(string key)
    {
        var baseValue = key == "press-01" ? 2.5 : 1.2;
        return (decimal)(baseValue + _random.NextDouble() * 7.2);
    }

    private int GenerateRpm(string key)
    {
        return key switch
        {
            "press-01" => 1200 + _random.Next(-120, 160),
            "line-01" => 850 + _random.Next(-90, 110),
            _ => 400 + _random.Next(-50, 60)
        };
    }

    private decimal GenerateEnergy(string key)
    {
        var baseValue = key == "press-01" ? 9.4 : 4.0;
        return (decimal)(baseValue + _random.NextDouble() * 4.5);
    }
}
