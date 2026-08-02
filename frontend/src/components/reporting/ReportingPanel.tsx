import type { AdminMachine, ConsumptionReport, ReportFilters, Zone } from '../../lib/types';

type Props = {
  filters: ReportFilters;
  report: ConsumptionReport | null;
  machines: AdminMachine[];
  zones: Zone[];
  loading: boolean;
  error: string | null;
  onChangeFilters: (filters: ReportFilters) => void;
  onExportCsv: () => void;
  onExportPdf: () => void;
};

export function ReportingPanel({ filters, report, machines, zones, loading, error, onChangeFilters, onExportCsv, onExportPdf }: Props) {
  const hasRows = report !== null && report.rows.length > 0;

  return (
    <div className="report-panel">
      <div className="page-head">
        <div>
          <p className="eyebrow">Analytics</p>
          <h1>Consumption reporting</h1>
          <p>Monthly energy and cost served from pre-aggregated windows, filterable by machine and zone.</p>
        </div>
        {error ? <span className="toast toast--error">{error}</span> : null}
      </div>

      <section className="panel">
        <div className="panel-header">
          <h2>Filters</h2>
          <span>{loading ? 'Loading…' : report ? `${report.rows.length} rows` : 'No data'}</span>
        </div>

        <div className="report-panel__filters">
          <label className="field">
            <span className="field__label">Month</span>
            <input type="month" value={filters.month} onChange={event => onChangeFilters({ ...filters, month: event.target.value })} />
          </label>
          <label className="field">
            <span className="field__label">Machine</span>
            <select value={filters.machineId} onChange={event => onChangeFilters({ ...filters, machineId: event.target.value })}>
              <option value="">All machines</option>
              {machines.map(machine => <option key={machine.machineId} value={machine.machineId}>{machine.name}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field__label">Zone</span>
            <select value={filters.zoneId} onChange={event => onChangeFilters({ ...filters, zoneId: event.target.value })}>
              <option value="">All zones</option>
              {zones.map(zone => <option key={zone.zoneId} value={zone.zoneId}>{zone.name}</option>)}
            </select>
          </label>
          <div className="report-panel__actions">
            <button type="button" className="btn" onClick={onExportCsv} disabled={!hasRows}>Export CSV</button>
            <button type="button" className="btn" onClick={onExportPdf} disabled={!hasRows}>Export PDF</button>
          </div>
        </div>
      </section>

      {report ? (
        <>
          <div className="stat-row">
            <div className="stat">
              <div className="stat__label">Total energy</div>
              <div className="stat__value">{report.totalKwh.toFixed(1)}<span className="stat__unit">kWh</span></div>
            </div>
            <div className="stat">
              <div className="stat__label">Total cost</div>
              <div className="stat__value">{report.totalCostEuro.toFixed(2)}<span className="stat__unit">€</span></div>
            </div>
            <div className="stat">
              <div className="stat__label">Rows</div>
              <div className="stat__value">{report.rows.length}</div>
            </div>
            <div className="stat">
              <div className="stat__label">Generated</div>
              <div className="stat__value" style={{ fontSize: 15 }}>{new Date(report.generatedAt).toLocaleString()}</div>
            </div>
          </div>

          <section className="panel">
            <div className="panel-header">
              <h2>Breakdown</h2>
              <span>{filters.month}</span>
            </div>

            {hasRows ? (
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Period</th>
                      <th>Scope</th>
                      <th>Label</th>
                      <th>Zone</th>
                      <th className="num">kWh</th>
                      <th className="num">Cost</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.rows.map(row => (
                      <tr key={`${row.scopeType}-${row.scopeId}-${row.periodStart}`}>
                        <td>{new Date(row.periodStart).toLocaleString()}</td>
                        <td><span className="badge badge--muted">{row.scopeType}</span></td>
                        <td className="strong">{row.label}</td>
                        <td>{row.zoneName ?? row.zoneId ?? '—'}</td>
                        <td className="num">{row.totalKwh.toFixed(2)}</td>
                        <td className="num">{row.costEuro.toFixed(2)} €</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state">
                No aggregates for {filters.month}. The hourly job writes a row once a full window has closed.
              </div>
            )}
          </section>
        </>
      ) : (
        <div className="empty-state">Pick a month to load the report.</div>
      )}
    </div>
  );
}
