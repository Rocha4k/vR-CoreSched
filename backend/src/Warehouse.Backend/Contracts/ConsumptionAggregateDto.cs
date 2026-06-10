namespace Warehouse.Backend.Contracts;

public sealed record ConsumptionAggregateDto(
    string Id,
    string ScopeType,
    string ScopeId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    decimal AverageKwh,
    decimal TotalKwh,
    decimal CostEuro);
