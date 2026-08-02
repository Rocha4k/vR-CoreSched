using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Backend.Data;

public static class WarehouseDbSeeder
{
    public static async Task SeedAsync(WarehouseDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Zones.AnyAsync(cancellationToken))
        {
            db.Zones.AddRange(
                new ZoneEntity { ZoneId = "loading-bay", Name = "Loading Bay", Description = "Goods receiving and dispatch.", Color = "#d8d8de", IsActive = true },
                new ZoneEntity { ZoneId = "production-area", Name = "Production Area", Description = "Main heavy machinery floor.", Color = "#b4b4bd", IsActive = true },
                new ZoneEntity { ZoneId = "assembly-line", Name = "Assembly Line", Description = "Assembly and finishing.", Color = "#9a9aa4", IsActive = true },
                new ZoneEntity { ZoneId = "aisle-a", Name = "Aisle A", Description = "Main aisle.", Color = "#80808b", IsActive = true },
                new ZoneEntity { ZoneId = "aisle-b", Name = "Aisle B", Description = "Secondary aisle.", Color = "#6a6a75", IsActive = true },
                new ZoneEntity { ZoneId = "offices", Name = "Offices", Description = "Administrative area.", Color = "#55555f", IsActive = true });
        }

        if (!await db.Machines.AnyAsync(cancellationToken))
        {
            db.Machines.AddRange(
                new MachineEntity { MachineId = "press-01", Name = "Hydraulic Press", ZoneId = "production-area", IsEnabled = true, IsOnline = true, LastSeen = DateTimeOffset.UtcNow, TemperatureC = 72, VibrationMs2 = 2.3m, Rpm = 1200, EnergyKwh = 9.1m, Severity = "Info", LocationX = 22, LocationY = 28 },
                new MachineEntity { MachineId = "line-01", Name = "Assembly Line", ZoneId = "assembly-line", IsEnabled = true, IsOnline = true, LastSeen = DateTimeOffset.UtcNow, TemperatureC = 66, VibrationMs2 = 1.8m, Rpm = 820, EnergyKwh = 6.4m, Severity = "Info", LocationX = 50, LocationY = 34 },
                new MachineEntity { MachineId = "belt-01", Name = "Conveyor Belt", ZoneId = "aisle-a", IsEnabled = true, IsOnline = true, LastSeen = DateTimeOffset.UtcNow, TemperatureC = 58, VibrationMs2 = 1.1m, Rpm = 400, EnergyKwh = 3.2m, Severity = "Info", LocationX = 65, LocationY = 45 });
        }

        if (!await db.LightingDevices.AnyAsync(cancellationToken))
        {
            db.LightingDevices.AddRange(
                new LightingDeviceEntity { DeviceId = "light-loading", ZoneId = "loading-bay", Name = "Loading Bay Light", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 14, LocationY = 16, IsVisible = true },
                new LightingDeviceEntity { DeviceId = "light-aisle-a", ZoneId = "aisle-a", Name = "Aisle A Light", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 42, LocationY = 42, IsVisible = true },
                new LightingDeviceEntity { DeviceId = "light-aisle-b", ZoneId = "aisle-b", Name = "Aisle B Light", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 72, LocationY = 42, IsVisible = true },
                new LightingDeviceEntity { DeviceId = "light-office", ZoneId = "offices", Name = "Office Light", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 83, LocationY = 16, IsVisible = true });
        }

        if (!await db.Rules.AnyAsync(cancellationToken))
        {
            db.Rules.AddRange(
                new RuleDefinitionEntity { Id = "rule-temp-vib-press", Code = "TEMP_VIB_001", Name = "Press critical on temperature and vibration", TargetType = "Machine", TargetId = "press-01", Severity = "Critical", TemperatureThreshold = 85, VibrationThreshold = 8, DurationSeconds = 5, CooldownSeconds = 30, IsEnabled = true },
                new RuleDefinitionEntity { Id = "rule-temp-vib-line", Code = "TEMP_VIB_002", Name = "Assembly line under stress", TargetType = "Machine", TargetId = "line-01", Severity = "Warning", TemperatureThreshold = 82, VibrationThreshold = 7, DurationSeconds = 6, CooldownSeconds = 30, IsEnabled = true },
                new RuleDefinitionEntity { Id = "rule-belt-overheat", Code = "TEMP_VIB_003", Name = "Conveyor belt overheating", TargetType = "Machine", TargetId = "belt-01", Severity = "Warning", TemperatureThreshold = 78, VibrationThreshold = 6, DurationSeconds = 8, CooldownSeconds = 60, IsEnabled = true });
        }

        if (!await db.Floorplans.AnyAsync(cancellationToken))
        {
            var floorplan = new FloorplanLayoutEntity
            {
                Id = 1,
                Name = "Main Warehouse",
                CanvasWidth = 1200,
                CanvasHeight = 760,
                TextureKey = "warehouse-grid",
                UpdatedAt = DateTimeOffset.UtcNow,
                BoundaryPointsJson = JsonSerializer.Serialize(new[]
                {
                    new FloorplanPoint(10, 14),
                    new FloorplanPoint(92, 14),
                    new FloorplanPoint(96, 26),
                    new FloorplanPoint(96, 86),
                    new FloorplanPoint(8, 86),
                    new FloorplanPoint(8, 24)
                })
            };

            floorplan.Pins.AddRange(new[]
            {
                new FloorplanPinEntity { Id = 1, DeviceType = "Light", DeviceId = "light-loading", Label = "Loading Bay Light", X = 14, Y = 16, IsVisible = true, ZoneId = "loading-bay" },
                new FloorplanPinEntity { Id = 2, DeviceType = "Light", DeviceId = "light-aisle-a", Label = "Aisle A Light", X = 42, Y = 42, IsVisible = true, ZoneId = "aisle-a" },
                new FloorplanPinEntity { Id = 3, DeviceType = "Light", DeviceId = "light-aisle-b", Label = "Aisle B Light", X = 72, Y = 42, IsVisible = true, ZoneId = "aisle-b" },
                new FloorplanPinEntity { Id = 4, DeviceType = "Light", DeviceId = "light-office", Label = "Office Light", X = 83, Y = 16, IsVisible = true, ZoneId = "offices" },
                new FloorplanPinEntity { Id = 5, DeviceType = "Machine", DeviceId = "press-01", Label = "Hydraulic Press", X = 22, Y = 28, IsVisible = true, ZoneId = "production-area" },
                new FloorplanPinEntity { Id = 6, DeviceType = "Machine", DeviceId = "line-01", Label = "Assembly Line", X = 50, Y = 34, IsVisible = true, ZoneId = "assembly-line" },
                new FloorplanPinEntity { Id = 7, DeviceType = "Machine", DeviceId = "belt-01", Label = "Conveyor Belt", X = 65, Y = 45, IsVisible = true, ZoneId = "aisle-a" }
            });

            db.Floorplans.Add(floorplan);
        }

        if (!await db.MaintenanceRecords.AnyAsync(cancellationToken))
        {
            db.MaintenanceRecords.Add(
                new MaintenanceRecordEntity
                {
                    Id = Guid.NewGuid(),
                    MachineId = "press-01",
                    AlertId = null,
                    Title = "Preventive press inspection",
                    Status = "Closed",
                    Notes = "Lubrication and inspection completed during system bring-up.",
                    CreatedBy = "system",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                    ClosedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    ClosedBy = "supervisor"
                });
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.AddRange(
                CreateUser("operator", "Line Operator", "Operator", "operator123"),
                CreateUser("supervisor", "Shift Supervisor", "Supervisor", "supervisor123"),
                CreateUser("admin", "System Administrator", "Admin", "admin123"));
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static AppUserEntity CreateUser(string username, string fullName, string role, string password)
    {
        var (salt, hash) = Warehouse.Backend.Security.PasswordHasher.HashPassword(password);
        return new AppUserEntity
        {
            Username = username,
            FullName = fullName,
            Role = role,
            IsActive = true,
            PasswordSalt = salt,
            PasswordHash = hash,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = null
        };
    }
}
