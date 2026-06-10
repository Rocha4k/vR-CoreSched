using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Data;
using System.Globalization;

namespace Warehouse.Backend.Infrastructure;

public sealed class PostgresWarehouseStore : IWarehouseStore
{
    private readonly IDbContextFactory<WarehouseDbContext> _dbContextFactory;

    public PostgresWarehouseStore(IDbContextFactory<WarehouseDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<MachineStateDto>> GetMachinesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Machines
            .OrderBy(item => item.Name)
            .Select(item => new MachineStateDto(item.MachineId, item.Name, item.ZoneId, item.IsOnline, item.LastSeen ?? DateTimeOffset.UtcNow, item.TemperatureC, item.VibrationMs2, item.Rpm, item.EnergyKwh, item.Severity))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LightingDeviceDto>> GetLightingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.LightingDevices
            .OrderBy(item => item.Name)
            .Select(item => new LightingDeviceDto(item.DeviceId, item.ZoneId, item.Name, item.IsOn, item.LastChangedAt, item.LastCommandSource))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Alerts
            .OrderByDescending(item => item.StartTime)
            .Select(item => new AlertDto(item.Id.ToString("N"), item.MachineId, item.Severity, item.RuleCode, item.Message, item.StartTime, item.EndTime, item.IsAcknowledged))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConsumptionAggregateDto>> GetAggregatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ConsumptionAggregates
            .OrderByDescending(item => item.PeriodStart)
            .Select(item => new ConsumptionAggregateDto(item.Id.ToString("N"), item.ScopeType, item.ScopeId, item.PeriodStart, item.PeriodEnd, item.AverageKwh, item.TotalKwh, item.CostEuro))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConsumptionReportDto> GetConsumptionReportAsync(string month, string? machineId, string? zoneId, CancellationToken cancellationToken = default)
    {
        if (!DateTimeOffset.TryParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var monthStart))
        {
            monthStart = new DateTimeOffset(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        var monthEnd = monthStart.AddMonths(1);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var aggregates = await db.ConsumptionAggregates
            .Where(item => item.PeriodStart >= monthStart && item.PeriodStart < monthEnd)
            .OrderByDescending(item => item.PeriodStart)
            .ToListAsync(cancellationToken);

        var machines = await db.Machines.ToListAsync(cancellationToken);
        var zones = await db.Zones.ToListAsync(cancellationToken);

        var rows = aggregates
            .Select(item =>
            {
                var machine = string.Equals(item.ScopeType, "Machine", StringComparison.OrdinalIgnoreCase)
                    ? machines.FirstOrDefault(candidate => candidate.MachineId == item.ScopeId)
                    : null;
                var zone = string.Equals(item.ScopeType, "Zone", StringComparison.OrdinalIgnoreCase)
                    ? zones.FirstOrDefault(candidate => candidate.ZoneId == item.ScopeId)
                    : machine is null ? null : zones.FirstOrDefault(candidate => candidate.ZoneId == machine.ZoneId);

                return new ConsumptionReportRowDto(
                    item.ScopeType,
                    item.ScopeId,
                    machine?.Name ?? zone?.Name ?? item.ScopeId,
                    machine?.MachineId,
                    machine?.Name,
                    zone?.ZoneId ?? machine?.ZoneId,
                    zone?.Name,
                    item.PeriodStart,
                    item.PeriodEnd,
                    item.AverageKwh,
                    item.TotalKwh,
                    item.CostEuro);
            })
            .Where(row =>
                (string.IsNullOrWhiteSpace(machineId) || string.Equals(row.MachineId, machineId, StringComparison.OrdinalIgnoreCase) || string.Equals(row.ScopeId, machineId, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(zoneId) || string.Equals(row.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase) || string.Equals(row.ScopeId, zoneId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(row => row.PeriodStart)
            .ToList();

        var totalKwh = rows.Sum(item => item.TotalKwh);
        var totalCost = rows.Sum(item => item.CostEuro);

        return new ConsumptionReportDto(month, machineId, zoneId, DateTimeOffset.UtcNow, totalKwh, totalCost, rows);
    }

    public async Task<IReadOnlyList<MaintenanceRecordDto>> GetMaintenanceHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MaintenanceRecords
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new MaintenanceRecordDto(item.Id.ToString("N"), item.MachineId, item.AlertId, item.Title, item.Status, item.Notes, item.CreatedBy, item.CreatedAt, item.ClosedAt, item.ClosedBy))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuleDefinitionDto>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Rules
            .OrderBy(item => item.Name)
            .Select(item => new RuleDefinitionDto(item.Id, item.Code, item.Name, item.TargetType, item.TargetId, item.Severity, item.TemperatureThreshold, item.VibrationThreshold, item.DurationSeconds, item.CooldownSeconds, item.IsEnabled))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Zones
            .OrderBy(item => item.Name)
            .Select(item => new AdminZoneDto(item.ZoneId, item.Name, item.Description, item.Color, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminMachineDto>> GetAdminMachinesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Machines
            .OrderBy(item => item.Name)
            .Select(item => new AdminMachineDto(item.MachineId, item.Name, item.ZoneId, item.IsEnabled, item.IsOnline, item.LastSeen, item.TemperatureC, item.VibrationMs2, item.Rpm, item.EnergyKwh, item.Severity, item.LocationX, item.LocationY))
            .ToListAsync(cancellationToken);
    }

    public async Task<FloorplanLayoutDto> GetFloorplanAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var floorplan = await db.Floorplans
            .Include(item => item.Pins)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstAsync(cancellationToken);

        return ToFloorplanDto(floorplan);
    }

    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var machines = await GetMachinesAsync(cancellationToken);
        var lighting = await GetLightingAsync(cancellationToken);
        var alerts = await GetAlertsAsync(cancellationToken);
        var aggregates = await GetAggregatesAsync(cancellationToken);
        var maintenance = await GetMaintenanceHistoryAsync(cancellationToken);
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
        var entity = new AlertEntity
        {
            Id = Guid.Parse(alert.Id),
            MachineId = alert.MachineId,
            Severity = alert.Severity,
            RuleCode = alert.RuleCode,
            Message = alert.Message,
            StartTime = alert.StartTime,
            EndTime = alert.EndTime,
            IsAcknowledged = alert.IsAcknowledged,
            AcknowledgedAt = null,
            AcknowledgedBy = null,
            AcknowledgementNote = null
        };

        db.Alerts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<AlertDto?> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, string? note, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!Guid.TryParse(alertId, out var parsedId))
        {
            return null;
        }

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

    public async Task<ConsumptionAggregateDto> AddAggregateAsync(ConsumptionAggregateDto aggregate, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ConsumptionAggregates.Add(new ConsumptionAggregateEntity
        {
            Id = Guid.Parse(aggregate.Id),
            ScopeType = aggregate.ScopeType,
            ScopeId = aggregate.ScopeId,
            PeriodStart = aggregate.PeriodStart,
            PeriodEnd = aggregate.PeriodEnd,
            AverageKwh = aggregate.AverageKwh,
            TotalKwh = aggregate.TotalKwh,
            CostEuro = aggregate.CostEuro
        });

        await db.SaveChangesAsync(cancellationToken);
        return aggregate;
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
        return await GetFloorplanAsync(cancellationToken);
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
        await db.SaveChangesAsync(cancellationToken);
        return pin;
    }

    public async Task<DateTimeOffset?> GetLastTelemetryAtAsync(string machineId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelemetryEvents
            .Where(item => item.MachineId == machineId)
            .OrderByDescending(item => item.Timestamp)
            .Select(item => (DateTimeOffset?)item.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MachineTelemetryDto>> GetRecentTelemetryAsync(string machineId, int take, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelemetryEvents
            .Join(db.Machines,
                telemetry => telemetry.MachineId,
                machine => machine.MachineId,
                (telemetry, machine) => new { telemetry, machine })
            .Where(item => item.telemetry.MachineId == machineId)
            .OrderByDescending(item => item.telemetry.Timestamp)
            .Take(take)
            .OrderBy(item => item.telemetry.Timestamp)
            .Select(item => new MachineTelemetryDto(item.telemetry.MachineId, item.machine.Name, item.machine.ZoneId, item.telemetry.Timestamp, item.telemetry.TemperatureC, item.telemetry.VibrationMs2, item.telemetry.Rpm, item.telemetry.EnergyKwh, item.telemetry.Source))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MachineTelemetryDto>> GetAllRecentTelemetryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelemetryEvents
            .Join(db.Machines,
                telemetry => telemetry.MachineId,
                machine => machine.MachineId,
                (telemetry, machine) => new { telemetry, machine })
            .OrderByDescending(item => item.telemetry.Timestamp)
            .Take(5000)
            .Select(item => new MachineTelemetryDto(item.telemetry.MachineId, item.machine.Name, item.machine.ZoneId, item.telemetry.Timestamp, item.telemetry.TemperatureC, item.telemetry.VibrationMs2, item.telemetry.Rpm, item.telemetry.EnergyKwh, item.telemetry.Source))
            .ToListAsync(cancellationToken);
    }

    public async Task SetMachineSeverityAsync(string machineId, string severity, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var machine = await db.Machines.FirstOrDefaultAsync(item => item.MachineId == machineId, cancellationToken);
        if (machine is null)
        {
            return;
        }

        machine.Severity = severity;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static FloorplanLayoutDto ToFloorplanDto(FloorplanLayoutEntity layout)
    {
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
}
