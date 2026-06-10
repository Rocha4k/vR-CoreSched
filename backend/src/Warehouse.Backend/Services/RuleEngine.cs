using System.Collections.Concurrent;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Infrastructure;

namespace Warehouse.Backend.Services;

public sealed class RuleEngine : IRuleEngine
{
    private readonly IWarehouseStore _store;
    private readonly ConcurrentDictionary<string, RuleWindow> _windows = new();

    public RuleEngine(IWarehouseStore store)
    {
        _store = store;
    }

    public async Task<AlertDto?> EvaluateTelemetryAsync(MachineTelemetryDto telemetry, CancellationToken cancellationToken = default)
    {
        var rules = await _store.GetRulesAsync(cancellationToken);
        var now = telemetry.Timestamp;

        foreach (var rule in rules.Where(item => item.IsEnabled &&
                                                 ((item.TargetType == "Machine" && string.Equals(item.TargetId, telemetry.MachineId, StringComparison.OrdinalIgnoreCase)) ||
                                                  (item.TargetType == "Zone" && string.Equals(item.TargetId, telemetry.Zone, StringComparison.OrdinalIgnoreCase)))))
        {
            var windowKey = $"{telemetry.MachineId}:{rule.Code}";
            var window = _windows.GetOrAdd(windowKey, _ => new RuleWindow());

            if (telemetry.TemperatureC > rule.TemperatureThreshold && telemetry.VibrationMs2 > rule.VibrationThreshold)
            {
                window.MarkHot(now);
                if (window.IsCriticalFor(TimeSpan.FromSeconds(rule.DurationSeconds), now) && window.CanAlert(now, rule.CooldownSeconds))
                {
                    var alert = new AlertDto(
                        Guid.NewGuid().ToString("N"),
                        telemetry.MachineId,
                        rule.Severity,
                        rule.Code,
                        $"{telemetry.Name} ultrapassou os limiares da regra {rule.Name}.",
                        now,
                        null,
                        false);

                    window.RegisterAlert(now);
                    await _store.SetMachineSeverityAsync(telemetry.MachineId, rule.Severity, cancellationToken);
                    return await _store.AddAlertAsync(alert, cancellationToken);
                }
            }

            window.ResetHot();
        }

        await _store.SetMachineSeverityAsync(telemetry.MachineId, "Info", cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<AlertDto>> EvaluateOfflineMachinesAsync(CancellationToken cancellationToken = default)
    {
        var machines = await _store.GetMachinesAsync(cancellationToken);
        var alerts = new List<AlertDto>();

        foreach (var machine in machines)
        {
            var lastSeen = await _store.GetLastTelemetryAtAsync(machine.MachineId, cancellationToken);
            if (lastSeen is null)
            {
                continue;
            }

            if (DateTimeOffset.UtcNow - lastSeen > TimeSpan.FromSeconds(10))
            {
                var alert = new AlertDto(
                    Guid.NewGuid().ToString("N"),
                    machine.MachineId,
                    "Warning",
                    "OFFLINE_001",
                    $"{machine.Name} não envia telemetria há mais de 10 segundos.",
                    DateTimeOffset.UtcNow,
                    null,
                    false);

                alerts.Add(await _store.AddAlertAsync(alert, cancellationToken));
                await _store.SetMachineSeverityAsync(machine.MachineId, "Warning", cancellationToken);
            }
        }

        return alerts;
    }

    private sealed class RuleWindow
    {
        private DateTimeOffset? _hotSince;
        private DateTimeOffset? _lastAlertAt;

        public void MarkHot(DateTimeOffset timestamp)
        {
            _hotSince ??= timestamp;
        }

        public void ResetHot()
        {
            _hotSince = null;
        }

        public bool IsCriticalFor(TimeSpan duration, DateTimeOffset now)
        {
            return _hotSince is not null && now - _hotSince >= duration;
        }

        public bool CanAlert(DateTimeOffset now, int cooldownSeconds)
        {
            return _lastAlertAt is null || now - _lastAlertAt > TimeSpan.FromSeconds(cooldownSeconds);
        }

        public void RegisterAlert(DateTimeOffset now)
        {
            _lastAlertAt = now;
        }
    }
}
