using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Infrastructure;

namespace Warehouse.Backend.Services;

public sealed class RuleEngine : IRuleEngine
{
    private const string DefaultSeverity = "Info";
    private const string OfflineRuleCode = "OFFLINE_001";

    private readonly IWarehouseStore _store;
    private readonly WarehouseOptions _options;
    private readonly ILogger<RuleEngine> _logger;
    private readonly ConcurrentDictionary<string, RuleWindow> _windows = new();
    private readonly ConcurrentDictionary<string, string> _lastSeverity = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastOfflineAlert = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _rulesLock = new(1, 1);

    private IReadOnlyList<RuleDefinitionDto> _rules = [];
    private DateTimeOffset _rulesLoadedAt = DateTimeOffset.MinValue;

    public RuleEngine(IWarehouseStore store, IOptions<WarehouseOptions> options, ILogger<RuleEngine> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public void InvalidateRules() => _rulesLoadedAt = DateTimeOffset.MinValue;

    public async Task<AlertDto?> EvaluateTelemetryAsync(MachineTelemetryDto telemetry, CancellationToken cancellationToken = default)
    {
        var rules = await GetRulesAsync(cancellationToken);
        var now = telemetry.Timestamp;
        AlertDto? raised = null;
        var severity = DefaultSeverity;

        foreach (var rule in rules)
        {
            if (!Matches(rule, telemetry))
            {
                continue;
            }

            var window = _windows.GetOrAdd($"{telemetry.MachineId}:{rule.Code}", _ => new RuleWindow());
            var breaching = telemetry.TemperatureC > rule.TemperatureThreshold && telemetry.VibrationMs2 > rule.VibrationThreshold;

            if (!breaching)
            {
                // The window only closes here. Resetting it on every message meant
                // DurationSeconds could never be reached.
                window.Reset();
                continue;
            }

            severity = rule.Severity;
            window.MarkBreaching(now);

            if (raised is not null || !window.HasBreachedFor(TimeSpan.FromSeconds(rule.DurationSeconds), now) || !window.CanAlert(now, rule.CooldownSeconds))
            {
                continue;
            }

            window.RegisterAlert(now);
            raised = await _store.AddAlertAsync(
                new AlertDto(
                    Guid.NewGuid().ToString("N"),
                    telemetry.MachineId,
                    rule.Severity,
                    rule.Code,
                    $"{telemetry.Name} exceeded the thresholds of rule {rule.Name}.",
                    now,
                    null,
                    false),
                cancellationToken);
        }

        await ApplySeverityAsync(telemetry.MachineId, severity, cancellationToken);
        return raised;
    }

    public async Task<IReadOnlyList<AlertDto>> EvaluateOfflineMachinesAsync(CancellationToken cancellationToken = default)
    {
        var threshold = TimeSpan.FromSeconds(Math.Max(1, _options.OfflineThresholdSeconds));
        var cooldown = TimeSpan.FromSeconds(Math.Max(1, _options.AlertCooldownSeconds));
        var now = DateTimeOffset.UtcNow;

        // A single query instead of one telemetry read per machine.
        var heartbeats = await _store.GetMachineHeartbeatsAsync(cancellationToken);
        var alerts = new List<AlertDto>();

        foreach (var machine in heartbeats)
        {
            if (machine.LastSeen is not { } lastSeen || now - lastSeen <= threshold)
            {
                _lastOfflineAlert.TryRemove(machine.MachineId, out _);
                continue;
            }

            await _store.SetMachineOfflineAsync(machine.MachineId, cancellationToken);
            await ApplySeverityAsync(machine.MachineId, "Warning", cancellationToken);

            // Without a cooldown a fresh alert was raised on every sweep, flooding the table.
            if (_lastOfflineAlert.TryGetValue(machine.MachineId, out var lastAlert) && now - lastAlert <= cooldown)
            {
                continue;
            }

            _lastOfflineAlert[machine.MachineId] = now;
            alerts.Add(await _store.AddAlertAsync(
                new AlertDto(
                    Guid.NewGuid().ToString("N"),
                    machine.MachineId,
                    "Warning",
                    OfflineRuleCode,
                    $"{machine.Name} has not reported telemetry for more than {threshold.TotalSeconds:0} seconds.",
                    now,
                    null,
                    false),
                cancellationToken));
        }

        return alerts;
    }

    private static bool Matches(RuleDefinitionDto rule, MachineTelemetryDto telemetry)
    {
        if (!rule.IsEnabled)
        {
            return false;
        }

        return rule.TargetType switch
        {
            "Machine" => string.Equals(rule.TargetId, telemetry.MachineId, StringComparison.OrdinalIgnoreCase),
            "Zone" => string.Equals(rule.TargetId, telemetry.Zone, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private async Task ApplySeverityAsync(string machineId, string severity, CancellationToken cancellationToken)
    {
        // The hot path takes one message per machine per second: only write to the
        // database when the severity actually changes.
        if (_lastSeverity.TryGetValue(machineId, out var current) && string.Equals(current, severity, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastSeverity[machineId] = severity;
        await _store.SetMachineSeverityAsync(machineId, severity, cancellationToken);
    }

    private async Task<IReadOnlyList<RuleDefinitionDto>> GetRulesAsync(CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(1, _options.RuleCacheSeconds));
        if (DateTimeOffset.UtcNow - _rulesLoadedAt < ttl)
        {
            return _rules;
        }

        await _rulesLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow - _rulesLoadedAt < ttl)
            {
                return _rules;
            }

            _rules = await _store.GetRulesAsync(cancellationToken);
            _rulesLoadedAt = DateTimeOffset.UtcNow;
            _logger.LogDebug("Rule cache reloaded with {Count} rules.", _rules.Count);
            return _rules;
        }
        finally
        {
            _rulesLock.Release();
        }
    }

    private sealed class RuleWindow
    {
        private DateTimeOffset? _breachingSince;
        private DateTimeOffset? _lastAlertAt;

        public void MarkBreaching(DateTimeOffset timestamp) => _breachingSince ??= timestamp;

        public void Reset() => _breachingSince = null;

        public bool HasBreachedFor(TimeSpan duration, DateTimeOffset now) => _breachingSince is { } since && now - since >= duration;

        public bool CanAlert(DateTimeOffset now, int cooldownSeconds) => _lastAlertAt is not { } last || now - last > TimeSpan.FromSeconds(cooldownSeconds);

        public void RegisterAlert(DateTimeOffset now) => _lastAlertAt = now;
    }
}
