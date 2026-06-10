import type { ConsumptionAggregate } from '../../lib/types';

type Props = {
  aggregates: ConsumptionAggregate[];
};

type Bucket = {
  label: string;
  energy: number;
  cost: number;
};

export function ConsumptionChart({ aggregates }: Props) {
  const buckets = buildMonthlyBuckets(aggregates);
  const maxEnergy = Math.max(1, ...buckets.map(bucket => bucket.energy));
  const maxCost = Math.max(1, ...buckets.map(bucket => bucket.cost));

  return (
    <div className="chart-card">
      <div className="panel-header">
        <h2>Consumo e custo mensal</h2>
        <span>{buckets.length} meses</span>
      </div>
      <svg viewBox="0 0 640 260" className="chart-card__svg" role="img" aria-label="Consumo e custo mensal">
        {buckets.map((bucket, index) => {
          const baseX = 60 + index * 120;
          const energyHeight = (bucket.energy / maxEnergy) * 160;
          const costHeight = (bucket.cost / maxCost) * 160;

          return (
            <g key={bucket.label}>
              <rect x={baseX} y={200 - energyHeight} width="28" height={energyHeight} rx="6" className="chart-card__bar chart-card__bar--energy" />
              <rect x={baseX + 34} y={200 - costHeight} width="28" height={costHeight} rx="6" className="chart-card__bar chart-card__bar--cost" />
              <text x={baseX - 2} y="222" className="chart-card__label">{bucket.label}</text>
              <text x={baseX} y={200 - energyHeight - 10} className="chart-card__value">{bucket.energy.toFixed(1)}</text>
              <text x={baseX + 34} y={200 - costHeight - 10} className="chart-card__value">{bucket.cost.toFixed(0)}€</text>
            </g>
          );
        })}
      </svg>
      <div className="chart-card__legend">
        <span><i className="legend legend--energy" />Energia kWh</span>
        <span><i className="legend legend--cost" />Custo €</span>
      </div>
    </div>
  );
}

function buildMonthlyBuckets(aggregates: ConsumptionAggregate[]): Bucket[] {
  const grouped = new Map<string, Bucket>();

  for (const aggregate of aggregates) {
    const monthKey = aggregate.periodStart.slice(0, 7);
    const current = grouped.get(monthKey) ?? { label: monthKey.slice(5), energy: 0, cost: 0 };
    current.energy += aggregate.totalKwh;
    current.cost += aggregate.costEuro;
    grouped.set(monthKey, current);
  }

  return [...grouped.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .slice(-6)
    .map(([, bucket]) => bucket);
}
