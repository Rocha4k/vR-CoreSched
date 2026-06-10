namespace Warehouse.Backend.Contracts;

public sealed record DashboardSnapshotDto(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<MachineStateDto> Machines,
    IReadOnlyList<LightingDeviceDto> Lighting,
    IReadOnlyList<AlertDto> Alerts,
    IReadOnlyList<ConsumptionAggregateDto> Aggregates,
    IReadOnlyList<MaintenanceRecordDto> MaintenanceRecords);
