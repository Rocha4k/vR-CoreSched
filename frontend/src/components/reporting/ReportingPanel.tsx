import type { ConsumptionReport, ReportFilters, Zone, AdminMachine } from '../../lib/types';

type Props = {
  filters: ReportFilters;
  report: ConsumptionReport | null;
  machines: AdminMachine[];
  zones: Zone[];
  loading: boolean;
  onChangeFilters: (filters: ReportFilters) => void;
  onExportCsv: () => void;
  onExportPdf: () => void;
};

export function ReportingPanel({ filters, report, machines, zones, loading, onChangeFilters, onExportCsv, onExportPdf }: Props) {
  return (
    <section className="panel report-panel">
      <div className="panel-header">
        <h2>Reporting</h2>
        <span>{loading ? 'a carregar...' : report ? `${report.rows.length} linhas` : 'sem dados'}</span>
      </div>

      <div className="report-panel__filters">
        <label>
          Mês
          <input type="month" value={filters.month} onChange={event => onChangeFilters({ ...filters, month: event.target.value })} />
        </label>
        <label>
          Máquina
          <select value={filters.machineId} onChange={event => onChangeFilters({ ...filters, machineId: event.target.value })}>
            <option value="">Todas</option>
            {machines.map(machine => <option key={machine.machineId} value={machine.machineId}>{machine.name}</option>)}
          </select>
        </label>
        <label>
          Zona
          <select value={filters.zoneId} onChange={event => onChangeFilters({ ...filters, zoneId: event.target.value })}>
            <option value="">Todas</option>
            {zones.map(zone => <option key={zone.zoneId} value={zone.zoneId}>{zone.name}</option>)}
          </select>
        </label>
        <div className="report-panel__actions">
          <button type="button" onClick={onExportCsv}>Exportar CSV</button>
          <button type="button" onClick={onExportPdf}>Exportar PDF</button>
        </div>
      </div>

      {report ? (
        <>
          <div className="hero-kpis hero-kpis--report">
            <div><strong>{report.totalKwh.toFixed(1)} kWh</strong><span>total filtrado</span></div>
            <div><strong>{report.totalCostEuro.toFixed(2)} €</strong><span>custo filtrado</span></div>
            <div><strong>{new Date(report.generatedAt).toLocaleString()}</strong><span>gerado em</span></div>
          </div>

          <div className="report-table">
            <div className="report-table__head">
              <span>Data</span>
              <span>Label</span>
              <span>Máquina</span>
              <span>Zona</span>
              <span>kWh</span>
              <span>Custo</span>
            </div>
            {report.rows.map(row => (
              <div key={`${row.scopeType}-${row.scopeId}-${row.periodStart}`} className="report-table__row">
                <span>{new Date(row.periodStart).toLocaleDateString()}</span>
                <span>{row.label}</span>
                <span>{row.machineName ?? row.machineId ?? '-'}</span>
                <span>{row.zoneName ?? row.zoneId ?? '-'}</span>
                <span>{row.totalKwh.toFixed(2)}</span>
                <span>{row.costEuro.toFixed(2)} €</span>
              </div>
            ))}
          </div>
        </>
      ) : (
        <p className="report-panel__empty">Selecione um mês para carregar o relatório.</p>
      )}
    </section>
  );
}
