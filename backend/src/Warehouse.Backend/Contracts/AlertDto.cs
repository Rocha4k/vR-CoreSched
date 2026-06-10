namespace Warehouse.Backend.Contracts;

public sealed record AlertDto(
    string Id,
    string MachineId,
    string Severity,
    string RuleCode,
    string Message,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    bool IsAcknowledged);

public sealed record AcknowledgeAlertRequestDto(string? Note);
