using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Data;

namespace Warehouse.Backend.Infrastructure;

public sealed class PostgresWarehouseStore : IWarehouseStore
{
    private readonly IDbContextFactory<WarehouseDbContext> _dbContextFactory;
    private readonly WarehouseOptions _options;

    public PostgresWarehouseStore(IDbContextFactory<WarehouseDbContext> dbContextFactory, IOptions<WarehouseOptions> options)
    {
        _dbContextFactory = dbContextFactory;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<MachineStateDto>> GetMachinesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryMachines(db).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LightingDeviceDto>> GetLightingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryLighting(db).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryAlerts(db, _options.SnapshotAlertLimit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConsumptionAggregateDto>> GetAggregatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryAggregates(db, _options.SnapshotAggregateLimit).ToListAsync(cancellationToken);
    }

    public async Task<ConsumptionReportDto> GetConsumptionReportAsync(string month, string? machineId, string? zoneId, CancellationToken cancellationToken = default)
    {
        var monthStart = ParseMonth(month);
        var monthEnd = monthStart.AddMonths(1);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // O mapa máquina -> zona é pequeno e evita um JOIN por linha do relatório.
        var machines = await db.Machines
            .AsNoTracking()
            .Select(item => new { item.MachineId, item.Name, item.ZoneId })
            .ToDictionaryAsync(item => item.MachineId, cancellationToken);

        var zones = await db.Zones
            .AsNoTracking()
            .Select(item => new { item.ZoneId, item.Name })
            .ToDictionaryAsync(item => item.ZoneId, cancellationToken);

        var query = db.ConsumptionAggregates
            .AsNoTracking()
            .Where(item => item.PeriodStart >= monthStart && item.PeriodStart < monthEnd);

        // Filtros empurrados para SQL: o âmbito máquina é direto, o âmbito zona
        // inclui também as máquinas dessa zona.
        if (!string.IsNullOrWhiteSpace(machineId))
        {
            query = query.Where(item => item.ScopeId == machineId);
        }

        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            var zoneMachineIds = machines.Values
                .Where(item => string.Equals(item.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.MachineId)
                .ToArray();

            query = query.Where(item => item.ScopeId == zoneId || zoneMachineIds.Contains(item.ScopeId));
        }

        var aggregates = await query
            .OrderByDescending(item => item.PeriodStart)
            .ToListAsync(cancellationToken);

        var rows = new List<ConsumptionReportRowDto>(aggregates.Count);
        foreach (var item in aggregates)
        {
            var isMachineScope = string.Equals(item.ScopeType, "Machine", StringComparison.OrdinalIgnoreCase);
            machines.TryGetValue(item.ScopeId, out var machine);
            if (!isMachineScope)
            {
                machine = null;
            }

            var resolvedZoneId = machine?.ZoneId ?? item.ScopeId;
            zones.TryGetValue(resolvedZoneId, out var zone);

            rows.Add(new ConsumptionReportRowDto(
                item.ScopeType,
                item.ScopeId,
                machine?.Name ?? zone?.Name ?? item.ScopeId,
                machine?.MachineId,
                machine?.Name,
                machine?.ZoneId ?? (zone is null ? null : resolvedZoneId),
                zone?.Name,
                item.PeriodStart,
                item.PeriodEnd,
                item.AverageKwh,
                item.TotalKwh,
                item.CostEuro));
        }

        return new ConsumptionReportDto(month, machineId, zoneId, DateTimeOffset.UtcNow, rows.Sum(item => item.TotalKwh), rows.Sum(item => item.CostEuro), rows);
    }

    public async Task<IReadOnlyList<MaintenanceRecordDto>> GetMaintenanceHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryMaintenance(db, _options.SnapshotMaintenanceLimit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuleDefinitionDto>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Rules
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new RuleDefinitionDto(item.Id, item.Code, item.Name, item.TargetType, item.TargetId, item.Severity, item.TemperatureThreshold, item.VibrationThreshold, item.DurationSeconds, item.CooldownSeconds, item.IsEnabled))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Zones
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new AdminZoneDto(item.ZoneId, item.Name, item.Description, item.Color, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminMachineDto>> GetAdminMachinesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Machines
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new AdminMachineDto(item.MachineId, item.Name, item.ZoneId, item.IsEnabled, item.IsOnline, item.LastSeen, item.TemperatureC, item.VibrationMs2, item.Rpm, item.EnergyKwh, item.Severity, item.LocationX, item.LocationY))
            .ToListAsync(cancellationToken);
    }

    public async Task<FloorplanLayoutDto> GetFloorplanAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryFloorplanAsync(db, cancellationToken);
    }

    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Um único DbContext (uma ligação) para as cinco listas do dashboard,
        // em vez de cinco contextos independentes.
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var machines = await QueryMachines(db).ToListAsync(cancellationToken);
        var lighting = await QueryLighting(db).ToListAsync(cancellationToken);
        var alerts = await QueryAlerts(db, _options.SnapshotAlertLimit).ToListAsync(cancellationToken);
        var aggregates = await QueryAggregates(db, _options.SnapshotAggregateLimit).ToListAsync(cancellationToken);
        var maintenance = await QueryMaintenance(db, _options.SnapshotMaintenanceLimit).ToListAsync(cancellationToken);

        return new DashboardSnapshotDto(DateTimeOffset.UtcNow, machines, lighting, alerts, aggregates, maintenance);
    }

    public async Task UpsertTelemetryAsync(MachineTelemetryDto telemetry, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.TelemetryEvents.Add(new TelemetryEventEntity
        {
            Id = Guid.NewGuid(),
            MachineId = telemetry.MachineId,
            Timestamp = telemetry.Timestamp,
            TemperatureC = telemetry.TemperatureC,
            VibrationMs2 = telemetry.VibrationMs2,
            Rpm = telemetry.Rpm,
            EnergyKwh = telemetry.EnergyKwh,
            Source = telemetry.Source
        });

        var machine = await db.Machines.FirstOrDefaultAsync(item => item.MachineId == telemetry.MachineId, cancellationToken);
        if (machine is null)
        {
            machine = new MachineEntity
            {
                MachineId = telemetry.MachineId,
                Name = telemetry.Name,
                ZoneId = telemetry.Zone,
                IsEnabled = true,
                LocationX = 10,
                LocationY = 10
            };
            db.Machines.Add(machine);
        }

        machine.Name = telemetry.Name;
        machine.ZoneId = telemetry.Zone;
        machine.IsOnline = true;
        machine.LastSeen = telemetry.Timestamp;
        machine.TemperatureC = telemetry.TemperatureC;
        machine.VibrationMs2 = telemetry.VibrationMs2;
        machine.Rpm = telemetry.Rpm;
        machine.EnergyKwh = telemetry.EnergyKwh;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LightingDeviceDto?> ToggleLightingAsync(string deviceId, string source, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var device = await db.LightingDevices.FirstOrDefaultAsync(item => item.DeviceId == deviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        device.IsOn = !device.IsOn;
        device.LastChangedAt = DateTimeOffset.UtcNow;
        device.LastCommandSource = source;
        await db.SaveChangesAsync(cancellationToken);
        return new LightingDeviceDto(device.DeviceId, device.ZoneId, device.Name, device.IsOn, device.LastChangedAt, device.LastCommandSource);
    }

    public async Task<LightingDeviceDto?> UpsertLightingStateAsync(LightingStateDto lighting, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var device = await db.LightingDevices.FirstOrDefaultAsync(item => item.DeviceId == lighting.DeviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        device.ZoneId = lighting.Zone;
        device.Name = lighting.Name;
        device.IsOn = lighting.IsOn;
        device.LastChangedAt = lighting.Timestamp;
        device.LastCommandSource = lighting.Source;
        await db.SaveChangesAsync(cancellationToken);
        return new LightingDeviceDto(device.DeviceId, device.ZoneId, device.Name, device.IsOn, device.LastChangedAt, device.LastCommandSource);
    }

    public async Task<AlertDto> AddAlertAsync(AlertDto alert, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Alerts.Add(new AlertEntity
        {
            Id = Guid.Parse(alert.Id),
            MachineId = alert.MachineId,
            Severity = alert.Severity,
            RuleCode = alert.RuleCode,
            Message = alert.Message,
            StartTime = alert.StartTime,
            EndTime = alert.EndTime,
            IsAcknowledged = alert.IsAcknowledged
        });

        await db.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<AlertDto?> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, string? note, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(alertId, out var parsedId))
        {
            return null;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alert = await db.Alerts.FirstOrDefaultAsync(item => item.Id == parsedId, cancellationToken);
        if (alert is null)
        {
            return null;
        }

        alert.IsAcknowledged = true;
        alert.AcknowledgedBy = acknowledgedBy;
        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
        alert.AcknowledgementNote = note;

        var alreadyCreated = await db.MaintenanceRecords.AnyAsync(item => item.AlertId == alertId, cancellationToken);
        if (!alreadyCreated && (string.Equals(alert.Severity, "Critical", StringComparison.OrdinalIgnoreCase) || string.Equals(alert.Severity, "Warning", StringComparison.OrdinalIgnoreCase)))
        {
            db.MaintenanceRecords.Add(new MaintenanceRecordEntity
            {
                Id = Guid.NewGuid(),
                MachineId = alert.MachineId,
                AlertId = alertId,
                Title = $"Manutenção gerada por alerta {alert.RuleCode}",
                Status = "Open",
                Notes = note ?? alert.Message,
                CreatedBy = acknowledgedBy,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AlertDto(alert.Id.ToString("N"), alert.MachineId, alert.Severity, alert.RuleCode, alert.Message, alert.StartTime, alert.EndTime, alert.IsAcknowledged);
    }

    public async Task<MaintenanceRecordDto> AddMaintenanceRecordAsync(CreateMaintenanceRecordDto record, string createdBy, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = new MaintenanceRecordEntity
        {
            Id = Guid.NewGuid(),
            MachineId = record.MachineId,
            AlertId = null,
            Title = record.Title,
            Status = record.Status,
            Notes = record.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.MaintenanceRecords.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new MaintenanceRecordDto(entity.Id.ToString("N"), entity.MachineId, entity.AlertId, entity.Title, entity.Status, entity.Notes, entity.CreatedBy, entity.CreatedAt, entity.ClosedAt, entity.ClosedBy);
    }

    public async Task<RuleDefinitionDto> UpsertRuleAsync(RuleDefinitionDto rule, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Rules.FirstOrDefaultAsync(item => item.Id == rule.Id, cancellationToken);
        if (entity is null)
        {
            entity = new RuleDefinitionEntity { Id = rule.Id };
            db.Rules.Add(entity);
        }

        entity.Code = rule.Code;
        entity.Name = rule.Name;
        entity.TargetType = rule.TargetType;
        entity.TargetId = rule.TargetId;
        entity.Severity = rule.Severity;
        entity.TemperatureThreshold = rule.TemperatureThreshold;
        entity.VibrationThreshold = rule.VibrationThreshold;
        entity.DurationSeconds = rule.DurationSeconds;
        entity.CooldownSeconds = rule.CooldownSeconds;
        entity.IsEnabled = rule.IsEnabled;
        await db.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<AdminMachineDto> UpsertMachineAsync(AdminMachineDto machine, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Machines.FirstOrDefaultAsync(item => item.MachineId == machine.MachineId, cancellationToken);
        if (entity is null)
        {
            entity = new MachineEntity { MachineId = machine.MachineId };
            db.Machines.Add(entity);
        }

        entity.Name = machine.Name;
        entity.ZoneId = machine.ZoneId;
        entity.IsEnabled = machine.IsEnabled;
        entity.IsOnline = machine.IsOnline;
        entity.LastSeen = machine.LastSeen;
        entity.TemperatureC = machine.TemperatureC;
        entity.VibrationMs2 = machine.VibrationMs2;
        entity.Rpm = machine.Rpm;
        entity.EnergyKwh = machine.EnergyKwh;
        entity.Severity = machine.Severity;
        entity.LocationX = machine.LocationX;
        entity.LocationY = machine.LocationY;
        await db.SaveChangesAsync(cancellationToken);
        return machine;
    }

    public async Task<AdminZoneDto> UpsertZoneAsync(AdminZoneDto zone, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Zones.FirstOrDefaultAsync(item => item.ZoneId == zone.ZoneId, cancellationToken);
        if (entity is null)
        {
            entity = new ZoneEntity { ZoneId = zone.ZoneId };
            db.Zones.Add(entity);
        }

        entity.Name = zone.Name;
        entity.Description = zone.Description;
        entity.Color = zone.Color;
        entity.IsActive = zone.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return zone;
    }

    public async Task<FloorplanLayoutDto> UpsertFloorplanAsync(FloorplanLayoutDto layout, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Floorplans.Include(item => item.Pins).FirstOrDefaultAsync(item => item.Id == layout.Id, cancellationToken);
        if (entity is null)
        {
            entity = new FloorplanLayoutEntity { Id = layout.Id };
            db.Floorplans.Add(entity);
        }

        entity.Name = layout.Name;
        entity.CanvasWidth = layout.CanvasWidth;
        entity.CanvasHeight = layout.CanvasHeight;
        entity.TextureKey = layout.TextureKey;
        entity.BoundaryPointsJson = layout.BoundaryPointsJson;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await QueryFloorplanAsync(db, cancellationToken);
    }

    public async Task<FloorplanPinDto> UpsertFloorplanPinAsync(FloorplanPinDto pin, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.FloorplanPins.FirstOrDefaultAsync(item => item.Id == pin.Id, cancellationToken);
        if (entity is null)
        {
            entity = new FloorplanPinEntity { Id = pin.Id, FloorplanLayoutId = 1 };
            db.FloorplanPins.Add(entity);
        }

        entity.DeviceType = pin.DeviceType;
        entity.DeviceId = pin.DeviceId;
        entity.Label = pin.Label;
        entity.X = pin.X;
        entity.Y = pin.Y;
        entity.IsVisible = pin.IsVisible;
        entity.ZoneId = pin.ZoneId;

        // A planta e o catálogo de máquinas partilham a posição do mesmo equipamento.
        if (string.Equals(pin.DeviceType, "Machine", StringComparison.OrdinalIgnoreCase))
        {
            var machine = await db.Machines.FirstOrDefaultAsync(item => item.MachineId == pin.DeviceId, cancellationToken);
            if (machine is not null)
            {
                machine.LocationX = pin.X;
                machine.LocationY = pin.Y;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return pin;
    }

    public async Task<IReadOnlyList<MachineHeartbeat>> GetMachineHeartbeatsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Machines
            .AsNoTracking()
            .Where(item => item.IsEnabled)
            .Select(item => new MachineHeartbeat(item.MachineId, item.Name, item.LastSeen))
            .ToListAsync(cancellationToken);
    }

    public async Task SetMachineSeverityAsync(string machineId, string severity, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.Machines
            .Where(item => item.MachineId == machineId && item.Severity != severity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Severity, severity), cancellationToken);
    }

    public async Task SetMachineOfflineAsync(string machineId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.Machines
            .Where(item => item.MachineId == machineId && item.IsOnline)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsOnline, false), cancellationToken);
    }

    public async Task<int> WriteConsumptionAggregatesAsync(DateTimeOffset periodStart, DateTimeOffset periodEnd, decimal euroPerKwh, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var window = db.TelemetryEvents
            .AsNoTracking()
            .Where(item => item.Timestamp >= periodStart && item.Timestamp < periodEnd);

        // Somas e médias calculadas em SQL (GROUP BY), sem trazer telemetria bruta para memória.
        var byMachine = await window
            .GroupBy(item => item.MachineId)
            .Select(group => new ScopeTotals("Machine", group.Key, group.Sum(item => item.EnergyKwh), group.Average(item => item.EnergyKwh)))
            .ToListAsync(cancellationToken);

        var byZone = await window
            .Join(db.Machines, item => item.MachineId, machine => machine.MachineId, (item, machine) => new { machine.ZoneId, item.EnergyKwh })
            .GroupBy(item => item.ZoneId)
            .Select(group => new ScopeTotals("Zone", group.Key, group.Sum(item => item.EnergyKwh), group.Average(item => item.EnergyKwh)))
            .ToListAsync(cancellationToken);

        var totals = byMachine.Concat(byZone).ToList();
        if (totals.Count == 0)
        {
            return 0;
        }

        // Idempotência: um novo arranque do worker não duplica a mesma janela.
        var existing = await db.ConsumptionAggregates
            .AsNoTracking()
            .Where(item => item.PeriodStart == periodStart)
            .Select(item => item.ScopeType + "|" + item.ScopeId)
            .ToListAsync(cancellationToken);

        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var inserted = 0;

        foreach (var scope in totals)
        {
            if (string.IsNullOrWhiteSpace(scope.ScopeId) || !known.Add($"{scope.ScopeType}|{scope.ScopeId}"))
            {
                continue;
            }

            db.ConsumptionAggregates.Add(new ConsumptionAggregateEntity
            {
                Id = Guid.NewGuid(),
                ScopeType = scope.ScopeType,
                ScopeId = scope.ScopeId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                AverageKwh = scope.Average,
                TotalKwh = scope.Total,
                CostEuro = decimal.Round(scope.Total * euroPerKwh, 4)
            });

            inserted++;
        }

        if (inserted > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return inserted;
    }

    public async Task<int> PurgeTelemetryOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelemetryEvents
            .Where(item => item.Timestamp < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static IQueryable<MachineStateDto> QueryMachines(WarehouseDbContext db) => db.Machines
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .Select(item => new MachineStateDto(item.MachineId, item.Name, item.ZoneId, item.IsOnline, item.LastSeen ?? DateTimeOffset.UtcNow, item.TemperatureC, item.VibrationMs2, item.Rpm, item.EnergyKwh, item.Severity));

    private static IQueryable<LightingDeviceDto> QueryLighting(WarehouseDbContext db) => db.LightingDevices
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .Select(item => new LightingDeviceDto(item.DeviceId, item.ZoneId, item.Name, item.IsOn, item.LastChangedAt, item.LastCommandSource));

    private static IQueryable<AlertDto> QueryAlerts(WarehouseDbContext db, int limit) => db.Alerts
        .AsNoTracking()
        .OrderByDescending(item => item.StartTime)
        .Take(limit)
        .Select(item => new AlertDto(item.Id.ToString("N"), item.MachineId, item.Severity, item.RuleCode, item.Message, item.StartTime, item.EndTime, item.IsAcknowledged));

    private static IQueryable<ConsumptionAggregateDto> QueryAggregates(WarehouseDbContext db, int limit) => db.ConsumptionAggregates
        .AsNoTracking()
        .OrderByDescending(item => item.PeriodStart)
        .Take(limit)
        .Select(item => new ConsumptionAggregateDto(item.Id.ToString("N"), item.ScopeType, item.ScopeId, item.PeriodStart, item.PeriodEnd, item.AverageKwh, item.TotalKwh, item.CostEuro));

    private static IQueryable<MaintenanceRecordDto> QueryMaintenance(WarehouseDbContext db, int limit) => db.MaintenanceRecords
        .AsNoTracking()
        .OrderByDescending(item => item.CreatedAt)
        .Take(limit)
        .Select(item => new MaintenanceRecordDto(item.Id.ToString("N"), item.MachineId, item.AlertId, item.Title, item.Status, item.Notes, item.CreatedBy, item.CreatedAt, item.ClosedAt, item.ClosedBy));

    private static async Task<FloorplanLayoutDto> QueryFloorplanAsync(WarehouseDbContext db, CancellationToken cancellationToken)
    {
        var layout = await db.Floorplans
            .AsNoTracking()
            .Include(item => item.Pins)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstAsync(cancellationToken);

        return new FloorplanLayoutDto(
            layout.Id,
            layout.Name,
            layout.CanvasWidth,
            layout.CanvasHeight,
            layout.TextureKey,
            layout.BoundaryPointsJson,
            layout.UpdatedAt,
            layout.Pins
                .OrderBy(item => item.Id)
                .Select(item => new FloorplanPinDto(item.Id, item.DeviceType, item.DeviceId, item.Label, item.X, item.Y, item.IsVisible, item.ZoneId))
                .ToList());
    }

    private static DateTimeOffset ParseMonth(string month)
    {
        if (DateTimeOffset.TryParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        var now = DateTime.UtcNow;
        return new DateTimeOffset(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private sealed record ScopeTotals(string ScopeType, string ScopeId, decimal Total, decimal Average);
}
