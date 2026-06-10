# Relatórios SQL

## Consultas úteis

### Máquina com maior consumo no mês

```sql
SELECT machine_id, SUM(total_kwh) AS total_kwh
FROM consumption_aggregate
WHERE period_start >= date_trunc('month', now())
GROUP BY machine_id
ORDER BY total_kwh DESC
LIMIT 1;
```

### Horário com maior desperdício de luz

```sql
SELECT date_part('hour', period_start) AS hour_of_day, SUM(total_kwh) AS total_kwh
FROM consumption_aggregate
WHERE scope_type = 'LightingZone'
GROUP BY hour_of_day
ORDER BY total_kwh DESC;
```

### Custo mensal em euros

```sql
SELECT SUM(total_kwh * 0.18) AS total_cost_eur
FROM consumption_aggregate
WHERE period_start >= date_trunc('month', now());

### Relatório filtrado por mês, máquina e zona

```sql
SELECT scope_type, scope_id, period_start, period_end, average_kwh, total_kwh, cost_euro
FROM consumption_aggregates
WHERE period_start >= date_trunc('month', DATE '2026-05-01')
	AND period_start < date_trunc('month', DATE '2026-05-01') + INTERVAL '1 month'
	AND (scope_id = 'press-01' OR 'press-01' IS NULL)
	AND (scope_id = 'zona-producao' OR 'zona-producao' IS NULL);
```

### Exportação no backend

- `GET /api/reports/consumption?month=YYYY-MM&machineId=...&zoneId=...`
- `GET /api/reports/consumption.csv?month=YYYY-MM&machineId=...&zoneId=...`
- `GET /api/reports/consumption.pdf?month=YYYY-MM&machineId=...&zoneId=...`
```
