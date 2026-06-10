namespace Warehouse.Backend.Contracts;

public sealed record LoginRequestDto(string Username, string Password);

public sealed record RefreshRequestDto(string RefreshToken);

public sealed record LoginResponseDto(string AccessToken, string RefreshToken, CurrentUserDto User);

public sealed record CurrentUserDto(string Username, string FullName, string Role, bool IsActive, DateTimeOffset? LastLoginAt);

public sealed record UserProfileDto(
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertUserRequestDto(
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    string? Password);

public sealed record UpdateProfileRequestDto(
    string FullName,
    string? CurrentPassword,
    string? NewPassword);

public sealed record MaintenanceRecordDto(
    string Id,
    string MachineId,
    string? AlertId,
    string Title,
    string Status,
    string Notes,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    string? ClosedBy);

public sealed record CreateMaintenanceRecordDto(
    string MachineId,
    string Title,
    string Notes,
    string Status);

public sealed record ConsumptionReportRequestDto(
    string Month,
    string? MachineId,
    string? ZoneId);

public sealed record ConsumptionReportRowDto(
    string ScopeType,
    string ScopeId,
    string Label,
    string? MachineId,
    string? MachineName,
    string? ZoneId,
    string? ZoneName,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    decimal AverageKwh,
    decimal TotalKwh,
    decimal CostEuro);

public sealed record ConsumptionReportDto(
    string Month,
    string? MachineId,
    string? ZoneId,
    DateTimeOffset GeneratedAt,
    decimal TotalKwh,
    decimal TotalCostEuro,
    IReadOnlyList<ConsumptionReportRowDto> Rows);
