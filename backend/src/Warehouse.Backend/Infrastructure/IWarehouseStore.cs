using Warehouse.Backend.Contracts;

namespace Warehouse.Backend.Infrastructure;

/// <summary>Instantâneo leve usado pela deteção de máquinas offline, sem carregar telemetria bruta.</summary>
public sealed record MachineHeartbeat(string MachineId, string Name, DateTimeOffset? LastSeen);

public interface IWarehouseStore
{
    Task<IReadOnlyList<MachineStateDto>> GetMachinesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LightingDeviceDto>> GetLightingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsumptionAggregateDto>> GetAggregatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceRecordDto>> GetMaintenanceHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleDefinitionDto>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminMachineDto>> GetAdminMachinesAsync(CancellationToken cancellationToken = default);
    Task<ConsumptionReportDto> GetConsumptionReportAsync(string month, string? machineId, string? zoneId, CancellationToken cancellationToken = default);
    Task<FloorplanLayoutDto> GetFloorplanAsync(CancellationToken cancellationToken = default);
    Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task UpsertTelemetryAsync(MachineTelemetryDto telemetry, CancellationToken cancellationToken = default);
    Task<LightingDeviceDto?> ToggleLightingAsync(string deviceId, string source, CancellationToken cancellationToken = default);
    Task<LightingDeviceDto?> UpsertLightingStateAsync(LightingStateDto lighting, CancellationToken cancellationToken = default);
    Task<AlertDto> AddAlertAsync(AlertDto alert, CancellationToken cancellationToken = default);
    Task<AlertDto?> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, string? note, CancellationToken cancellationToken = default);
    Task<MaintenanceRecordDto> AddMaintenanceRecordAsync(CreateMaintenanceRecordDto record, string createdBy, CancellationToken cancellationToken = default);
    Task<RuleDefinitionDto> UpsertRuleAsync(RuleDefinitionDto rule, CancellationToken cancellationToken = default);
    Task<AdminMachineDto> UpsertMachineAsync(AdminMachineDto machine, CancellationToken cancellationToken = default);
    Task<AdminZoneDto> UpsertZoneAsync(AdminZoneDto zone, CancellationToken cancellationToken = default);
    Task<FloorplanLayoutDto> UpsertFloorplanAsync(FloorplanLayoutDto layout, CancellationToken cancellationToken = default);
    Task<FloorplanPinDto> UpsertFloorplanPinAsync(FloorplanPinDto pin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MachineHeartbeat>> GetMachineHeartbeatsAsync(CancellationToken cancellationToken = default);
    Task SetMachineSeverityAsync(string machineId, string severity, CancellationToken cancellationToken = default);
    Task SetMachineOfflineAsync(string machineId, CancellationToken cancellationToken = default);

    /// <summary>Agrega consumo por máquina e por zona diretamente em SQL para a janela indicada.</summary>
    Task<int> WriteConsumptionAggregatesAsync(DateTimeOffset periodStart, DateTimeOffset periodEnd, decimal euroPerKwh, CancellationToken cancellationToken = default);

    /// <summary>Remove telemetria bruta anterior ao limite e devolve o número de linhas eliminadas.</summary>
    Task<int> PurgeTelemetryOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
