import type { ConsumptionAggregate } from '../../lib/types';

type Props = {
  aggregates: ConsumptionAggregate[];
};

type Bucket = {
  label: string;
  energy: number;
  cost: number;
};

const chartWidth = 640;
const chartHeight = 250;
const plotTop = 16;
const plotBottom = 208;
const plotLeft = 16;
const plotRight = chartWidth - 16;

export function ConsumptionChart({ aggregates }: Props) {
  const buckets = buildMonthlyBuckets(aggregates);
  const maxEnergy = Math.max(1, ...buckets.map(bucket => bucket.energy));
  const maxCost = Math.max(1, ...buckets.map(bucket => bucket.cost));
  const plotHeight = plotBottom - plotTop;
  const slot = buckets.length > 0 ? (plotRight - plotLeft) / buckets.length : 0;
  const barWidth = Math.min(26, slot / 3);

  return (
    <div className="panel chart-card">
      <div className="panel-header">
        <h2>Monthly consumption</h2>
        <span>{buckets.length === 1 ? '1 month' : `${buckets.length} months`}</span>
      </div>

      {buckets.length === 0 ? (
        <div className="empty-state">No aggregates yet — the hourly job has not produced a window.</div>
      ) : (
        <>
          <svg viewBox={`0 0 ${chartWidth} ${chartHeight}`} className="chart-card__svg" role="img" aria-label="Monthly energy and cost">
            {[0, 0.25, 0.5, 0.75, 1].map(step => {
              const y = plotBottom - step * plotHeight;
              return <line key={step} x1={plotLeft} y1={y} x2={plotRight} y2={y} className="chart-card__grid" />;
            })}

            {buckets.map((bucket, index) => {
              const slotStart = plotLeft + index * slot;
              const groupCenter = slotStart + slot / 2;
              const energyHeight = (bucket.energy / maxEnergy) * plotHeight;
              const costHeight = (bucket.cost / maxCost) * plotHeight;
              const energyX = groupCenter - barWidth - 3;
              const costX = groupCenter + 3;

              return (
                <g key={bucket.label}>
                  <rect x={energyX} y={plotBottom - energyHeight} width={barWidth} height={energyHeight} rx="3" className="chart-card__bar--energy" />
                  <rect x={costX} y={plotBottom - costHeight} width={barWidth} height={costHeight} rx="3" className="chart-card__bar--cost" />
                  <text x={energyX + barWidth / 2} y={plotBottom - energyHeight - 6} textAnchor="middle" className="chart-card__value">
                    {bucket.energy.toFixed(0)}
                  </text>
                  <text x={costX + barWidth / 2} y={plotBottom - costHeight - 6} textAnchor="middle" className="chart-card__value">
                    {bucket.cost.toFixed(0)}
                  </text>
                  <text x={groupCenter} y={plotBottom + 20} textAnchor="middle" className="chart-card__label">
                    {bucket.label}
                  </text>
                </g>
              );
            })}
          </svg>

          <div className="chart-card__legend">
            <span><i className="legend legend--energy" />Energy (kWh)</span>
            <span><i className="legend legend--cost" />Cost (€)</span>
          </div>
        </>
      )}
    </div>
  );
}

function buildMonthlyBuckets(aggregates: ConsumptionAggregate[]): Bucket[] {
  const grouped = new Map<string, Bucket>();

  // Machine and zone scopes cover the same energy, so counting both would double it.
  for (const aggregate of aggregates.filter(item => item.scopeType === 'Machine')) {
    const monthKey = aggregate.periodStart.slice(0, 7);
    const current = grouped.get(monthKey) ?? { label: formatMonth(monthKey), energy: 0, cost: 0 };
    current.energy += aggregate.totalKwh;
    current.cost += aggregate.costEuro;
    grouped.set(monthKey, current);
  }

  return [...grouped.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .slice(-6)
    .map(([, bucket]) => bucket);
}

function formatMonth(monthKey: string): string {
  const [year, month] = monthKey.split('-');
  const date = new Date(Number(year), Number(month) - 1, 1);
  return date.toLocaleString('en-GB', { month: 'short' });
}
