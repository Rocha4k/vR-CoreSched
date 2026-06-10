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
                new ZoneEntity { ZoneId = "zona-carga", Name = "Zona de Carga", Description = "Área de receção e expedição.", Color = "#f59e0b", IsActive = true },
                new ZoneEntity { ZoneId = "zona-producao", Name = "Zona de Produção", Description = "Área principal das máquinas pesadas.", Color = "#22c55e", IsActive = true },
                new ZoneEntity { ZoneId = "linha-montagem", Name = "Linha de Montagem", Description = "Montagem e acabamento.", Color = "#38bdf8", IsActive = true },
                new ZoneEntity { ZoneId = "corredor-a", Name = "Corredor A", Description = "Corredor principal.", Color = "#a78bfa", IsActive = true },
                new ZoneEntity { ZoneId = "corredor-b", Name = "Corredor B", Description = "Corredor secundário.", Color = "#f97316", IsActive = true },
                new ZoneEntity { ZoneId = "escritorios", Name = "Escritórios", Description = "Zona administrativa.", Color = "#f43f5e", IsActive = true });
        }

        if (!await db.Machines.AnyAsync(cancellationToken))
        {
            db.Machines.AddRange(
                new MachineEntity { MachineId = "press-01", Name = "Prensa Hidráulica", ZoneId = "zona-producao", IsEnabled = true, IsOnline = true, LastSeen = DateTimeOffset.UtcNow, TemperatureC = 72, VibrationMs2 = 2.3m, Rpm = 1200, EnergyKwh = 9.1m, Severity = "Info", LocationX = 22, LocationY = 28 },
                new MachineEntity { MachineId = "line-01", Name = "Linha de Montagem", ZoneId = "linha-montagem", IsEnabled = true, IsOnline = true, LastSeen = DateTimeOffset.UtcNow, TemperatureC = 66, VibrationMs2 = 1.8m, Rpm = 820, EnergyKwh = 6.4m, Severity = "Info", LocationX = 50, LocationY = 34 },
                new MachineEntity { MachineId = "belt-01", Name = "Tapete Rolante", ZoneId = "corredor-a", IsEnabled = true, IsOnline = true, LastSeen = DateTimeOffset.UtcNow, TemperatureC = 58, VibrationMs2 = 1.1m, Rpm = 400, EnergyKwh = 3.2m, Severity = "Info", LocationX = 65, LocationY = 45 });
        }

        if (!await db.LightingDevices.AnyAsync(cancellationToken))
        {
            db.LightingDevices.AddRange(
                new LightingDeviceEntity { DeviceId = "light-carga", ZoneId = "zona-carga", Name = "Luz da Zona de Carga", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 14, LocationY = 16, IsVisible = true },
                new LightingDeviceEntity { DeviceId = "light-corridor-a", ZoneId = "corredor-a", Name = "Luz do Corredor A", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 42, LocationY = 42, IsVisible = true },
                new LightingDeviceEntity { DeviceId = "light-corridor-b", ZoneId = "corredor-b", Name = "Luz do Corredor B", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 72, LocationY = 42, IsVisible = true },
                new LightingDeviceEntity { DeviceId = "light-office", ZoneId = "escritorios", Name = "Luz dos Escritórios", IsOn = true, LastChangedAt = DateTimeOffset.UtcNow, LastCommandSource = "seed", LocationX = 83, LocationY = 16, IsVisible = true });
        }

        if (!await db.Rules.AnyAsync(cancellationToken))
        {
            db.Rules.AddRange(
                new RuleDefinitionEntity { Id = "rule-temp-vib-press", Code = "TEMP_VIB_001", Name = "Prensa crítica por temperatura e vibração", TargetType = "Machine", TargetId = "press-01", Severity = "Critical", TemperatureThreshold = 85, VibrationThreshold = 8, DurationSeconds = 5, CooldownSeconds = 30, IsEnabled = true },
                new RuleDefinitionEntity { Id = "rule-temp-vib-line", Code = "TEMP_VIB_002", Name = "Linha de montagem sob stress", TargetType = "Machine", TargetId = "line-01", Severity = "Warning", TemperatureThreshold = 82, VibrationThreshold = 7, DurationSeconds = 6, CooldownSeconds = 30, IsEnabled = true },
                new RuleDefinitionEntity { Id = "rule-light-off-hours", Code = "LIGHT_WASTE_001", Name = "Luz fora de horário", TargetType = "Zone", TargetId = "corredor-a", Severity = "Info", TemperatureThreshold = 0, VibrationThreshold = 0, DurationSeconds = 0, CooldownSeconds = 60, IsEnabled = true });
        }

        if (!await db.Floorplans.AnyAsync(cancellationToken))
        {
            var floorplan = new FloorplanLayoutEntity
            {
                Id = 1,
                Name = "Armazém Principal",
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
                new FloorplanPinEntity { Id = 1, DeviceType = "Light", DeviceId = "light-carga", Label = "Luz da Zona de Carga", X = 14, Y = 16, IsVisible = true, ZoneId = "zona-carga" },
                new FloorplanPinEntity { Id = 2, DeviceType = "Light", DeviceId = "light-corridor-a", Label = "Luz do Corredor A", X = 42, Y = 42, IsVisible = true, ZoneId = "corredor-a" },
                new FloorplanPinEntity { Id = 3, DeviceType = "Light", DeviceId = "light-corridor-b", Label = "Luz do Corredor B", X = 72, Y = 42, IsVisible = true, ZoneId = "corredor-b" },
                new FloorplanPinEntity { Id = 4, DeviceType = "Light", DeviceId = "light-office", Label = "Luz dos Escritórios", X = 83, Y = 16, IsVisible = true, ZoneId = "escritorios" },
                new FloorplanPinEntity { Id = 5, DeviceType = "Machine", DeviceId = "press-01", Label = "Prensa Hidráulica", X = 22, Y = 28, IsVisible = true, ZoneId = "zona-producao" },
                new FloorplanPinEntity { Id = 6, DeviceType = "Machine", DeviceId = "line-01", Label = "Linha de Montagem", X = 50, Y = 34, IsVisible = true, ZoneId = "linha-montagem" },
                new FloorplanPinEntity { Id = 7, DeviceType = "Machine", DeviceId = "belt-01", Label = "Tapete Rolante", X = 65, Y = 45, IsVisible = true, ZoneId = "corredor-a" }
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
                    Title = "Verificação preventiva da prensa",
                    Status = "Closed",
                    Notes = "Lubrificação e inspeção concluídas no arranque do sistema.",
                    CreatedBy = "system",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                    ClosedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    ClosedBy = "supervisor"
                });
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.AddRange(
                CreateUser("operator", "Operador de Linha", "Operator", "operator123"),
                CreateUser("supervisor", "Supervisor de Turno", "Supervisor", "supervisor123"),
                CreateUser("admin", "Administrador do Sistema", "Admin", "admin123"));
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
