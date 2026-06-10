using Warehouse.Backend.Contracts;

namespace Warehouse.Backend.Infrastructure;

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
    Task<ConsumptionAggregateDto> AddAggregateAsync(ConsumptionAggregateDto aggregate, CancellationToken cancellationToken = default);
    Task<MaintenanceRecordDto> AddMaintenanceRecordAsync(CreateMaintenanceRecordDto record, string createdBy, CancellationToken cancellationToken = default);
    Task<RuleDefinitionDto> UpsertRuleAsync(RuleDefinitionDto rule, CancellationToken cancellationToken = default);
    Task<AdminMachineDto> UpsertMachineAsync(AdminMachineDto machine, CancellationToken cancellationToken = default);
    Task<AdminZoneDto> UpsertZoneAsync(AdminZoneDto zone, CancellationToken cancellationToken = default);
    Task<FloorplanLayoutDto> UpsertFloorplanAsync(FloorplanLayoutDto layout, CancellationToken cancellationToken = default);
    Task<FloorplanPinDto> UpsertFloorplanPinAsync(FloorplanPinDto pin, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLastTelemetryAtAsync(string machineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MachineTelemetryDto>> GetRecentTelemetryAsync(string machineId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MachineTelemetryDto>> GetAllRecentTelemetryAsync(CancellationToken cancellationToken = default);
    Task SetMachineSeverityAsync(string machineId, string severity, CancellationToken cancellationToken = default);
}
