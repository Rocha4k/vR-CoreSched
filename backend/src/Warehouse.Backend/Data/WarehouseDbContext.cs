using Microsoft.EntityFrameworkCore;

namespace Warehouse.Backend.Data;

public sealed class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
    {
    }

    public DbSet<MachineEntity> Machines => Set<MachineEntity>();
    public DbSet<LightingDeviceEntity> LightingDevices => Set<LightingDeviceEntity>();
    public DbSet<TelemetryEventEntity> TelemetryEvents => Set<TelemetryEventEntity>();
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<ConsumptionAggregateEntity> ConsumptionAggregates => Set<ConsumptionAggregateEntity>();
    public DbSet<RuleDefinitionEntity> Rules => Set<RuleDefinitionEntity>();
    public DbSet<ZoneEntity> Zones => Set<ZoneEntity>();
    public DbSet<FloorplanLayoutEntity> Floorplans => Set<FloorplanLayoutEntity>();
    public DbSet<FloorplanPinEntity> FloorplanPins => Set<FloorplanPinEntity>();
    public DbSet<MaintenanceRecordEntity> MaintenanceRecords => Set<MaintenanceRecordEntity>();
    public DbSet<AppUserEntity> Users => Set<AppUserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MachineEntity>(entity =>
        {
            entity.ToTable("machines");
            entity.HasKey(item => item.MachineId);
            entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
            entity.Property(item => item.ZoneId).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<LightingDeviceEntity>(entity =>
        {
            entity.ToTable("lighting_devices");
            entity.HasKey(item => item.DeviceId);
            entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
            entity.Property(item => item.ZoneId).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<TelemetryEventEntity>(entity =>
        {
            entity.ToTable("telemetry_events");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.MachineId, item.Timestamp });
        });

        modelBuilder.Entity<AlertEntity>(entity =>
        {
            entity.ToTable("alerts");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.MachineId, item.StartTime });
            entity.Property(item => item.AcknowledgedBy).HasMaxLength(120);
            entity.Property(item => item.AcknowledgementNote).HasMaxLength(500);
        });

        modelBuilder.Entity<ConsumptionAggregateEntity>(entity =>
        {
            entity.ToTable("consumption_aggregates");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ScopeType, item.ScopeId, item.PeriodStart });
        });

        modelBuilder.Entity<RuleDefinitionEntity>(entity =>
        {
            entity.ToTable("rules");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<ZoneEntity>(entity =>
        {
            entity.ToTable("zones");
            entity.HasKey(item => item.ZoneId);
            entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<FloorplanLayoutEntity>(entity =>
        {
            entity.ToTable("floorplans");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Ignore(item => item.BoundaryPoints);
            entity.HasMany(item => item.Pins)
                .WithOne(item => item.FloorplanLayout!)
                .HasForeignKey(item => item.FloorplanLayoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FloorplanPinEntity>(entity =>
        {
            entity.ToTable("floorplan_pins");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.DeviceType).HasMaxLength(30).IsRequired();
            entity.Property(item => item.DeviceId).HasMaxLength(120).IsRequired();
            entity.Property(item => item.Label).HasMaxLength(120).IsRequired();
            entity.Property(item => item.ZoneId).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<MaintenanceRecordEntity>(entity =>
        {
            entity.ToTable("maintenance_records");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(180).IsRequired();
            entity.Property(item => item.CreatedBy).HasMaxLength(120).IsRequired();
            entity.Property(item => item.ClosedBy).HasMaxLength(120);
            entity.HasIndex(item => new { item.MachineId, item.CreatedAt });
        });

        modelBuilder.Entity<AppUserEntity>(entity =>
        {
            entity.ToTable("app_users");
            entity.HasKey(item => item.Username);
            entity.Property(item => item.Username).HasMaxLength(120).IsRequired();
            entity.Property(item => item.FullName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Role).HasMaxLength(40).IsRequired();
            entity.Property(item => item.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(item => item.PasswordSalt).HasMaxLength(256).IsRequired();
            entity.HasMany(item => item.RefreshTokens)
                .WithOne(item => item.User!)
                .HasForeignKey(item => item.Username)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ReplacedByTokenHash).HasMaxLength(128);
            entity.HasIndex(item => new { item.Username, item.TokenHash }).IsUnique();
            entity.HasIndex(item => item.ExpiresAt);
        });
    }
}
