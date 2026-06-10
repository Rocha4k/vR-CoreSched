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
    private readonly SimulatorOptions _options;
    private readonly Random _random = new();
    private readonly IReadOnlyList<MachineProfile> _machines;
    private readonly IReadOnlyList<LightingState> _lighting;

    public MachineSimulationWorker(IOptions<SimulatorOptions> options)
    {
        _options = options.Value;
        _machines = new[]
        {
            new MachineProfile("press-01", "Prensa Hidráulica", "zona-producao"),
            new MachineProfile("line-01", "Linha de Montagem", "linha-montagem"),
            new MachineProfile("belt-01", "Tapete Rolante", "corredor-a")
        };
        _lighting = new[]
        {
            new LightingState("light-carga", "zona-carga", "Luz da Zona de Carga", true, DateTimeOffset.UtcNow, "seed"),
            new LightingState("light-corridor-a", "corredor-a", "Luz do Corredor A", true, DateTimeOffset.UtcNow, "seed"),
            new LightingState("light-corridor-b", "corredor-b", "Luz do Corredor B", true, DateTimeOffset.UtcNow, "seed"),
            new LightingState("light-office", "escritorios", "Luz dos Escritórios", true, DateTimeOffset.UtcNow, "seed")
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithCleanSession()
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(options, stoppingToken);
                }

                await PublishTelemetryAsync(client, stoppingToken);
                await PublishLightingAsync(client, stoppingToken);
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PublishIntervalSeconds)), stoppingToken);
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

            var topic = $"warehouse/machines/{machine.MachineId}/telemetry";
            var payload = JsonSerializer.SerializeToUtf8Bytes(telemetry, JsonOptions);
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();
            await client.PublishAsync(message, cancellationToken);
        }
    }

    private async Task PublishLightingAsync(IMqttClient client, CancellationToken cancellationToken)
    {
        foreach (var item in _lighting)
        {
            var shouldBeOn = _random.Next(0, 100) > 10;
            var state = item with
            {
                IsOn = shouldBeOn,
                Timestamp = DateTimeOffset.UtcNow,
                Source = "simulator"
            };

            var topic = $"warehouse/lighting/{item.DeviceId}/state";
            var payload = JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions);
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();
            await client.PublishAsync(message, cancellationToken);
        }
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
