import { AlertStrip } from '../AlertStrip';
import { MachineCard } from '../MachineCard';
import { WarehouseMap } from '../WarehouseMap';
import { ConsumptionChart } from '../analytics/ConsumptionChart';
import { MaintenancePanel } from '../maintenance/MaintenancePanel';
import type { WorkspaceSnapshot } from '../../lib/types';

type Props = {
  workspace: WorkspaceSnapshot;
  connectionStatus: string;
  onToggleLight: (deviceId: string) => void;
  onAcknowledgeAlert: (alertId: string, note: string) => void;
  onCreateMaintenance: (machineId: string, title: string, notes: string, status: string) => Promise<void>;
  canCreateMaintenance: boolean;
};

export function DashboardView({ workspace, connectionStatus, onToggleLight, onAcknowledgeAlert, onCreateMaintenance, canCreateMaintenance }: Props) {
  const { dashboard, floorplan, machines, zones } = workspace;
  const totalEnergy = dashboard.aggregates.reduce((sum, item) => sum + item.totalKwh, 0);
  const totalCost = dashboard.aggregates.reduce((sum, item) => sum + item.costEuro, 0);
  const onlineMachines = dashboard.machines.filter(machine => machine.isOnline).length;

  return (
    <>
      <section className="hero">
        <div>
          <p className="eyebrow">vR-CoreSched</p>
          <h1>Fábrica simulada com telemetria, alertas e controlo em tempo real.</h1>
          <p className="lead">A versão atual já fala com PostgreSQL, SignalR e MQTT, e agora o painel passou a ter uma planta editável com a mesma linguagem visual para qualquer armazém.</p>
        </div>
        <div className="hero-kpis">
          <div><strong>{onlineMachines}</strong><span>máquinas online</span></div>
          <div><strong>{totalEnergy.toFixed(1)} kWh</strong><span>energia agregada</span></div>
          <div><strong>{totalCost.toFixed(2)} €</strong><span>custo estimado</span></div>
        </div>
      </section>

      <div className="status-strip">
        <span>{connectionStatus}</span>
        <span>{zones.length} zonas</span>
        <span>{floorplan.pins.length} pontos mapeados</span>
      </div>

      <AlertStrip alerts={dashboard.alerts} />

      <section className="dashboard-grid">
        <div className="panel panel-map">
          <div className="panel-header">
            <h2>Planta interativa</h2>
            <span>tempo real</span>
          </div>
          <WarehouseMap layout={floorplan} machines={machines} lighting={dashboard.lighting} />
          <div className="map-actions">
            {dashboard.lighting.map(light => (
              <button key={light.id} type="button" className={`map-action ${light.isOn ? 'is-on' : 'is-off'}`} onClick={() => onToggleLight(light.id)}>
                {light.name}
              </button>
            ))}
          </div>
        </div>

        <div className="panel panel-machines">
          <div className="panel-header">
            <h2>Máquinas</h2>
            <span>{dashboard.machines.length} ativos</span>
          </div>
          <div className="machine-list">
            {dashboard.machines.map(machine => <MachineCard key={machine.machineId} machine={machine} />)}
          </div>
        </div>
      </section>

      <section className="dashboard-grid dashboard-grid--lower">
        <ConsumptionChart aggregates={dashboard.aggregates} />
        <MaintenancePanel
          maintenanceRecords={dashboard.maintenanceRecords}
          machines={machines}
          canCreate={canCreateMaintenance}
          onCreateMaintenance={onCreateMaintenance}
        />
      </section>

      <section className="panel alerts-panel">
        <div className="panel-header">
          <h2>Alertas e acknowledge</h2>
          <span>{dashboard.alerts.length} alertas</span>
        </div>
        <div className="alerts-panel__list">
          {dashboard.alerts.map(alert => (
            <article key={alert.id} className={`alert-card ${alert.severity.toLowerCase()}`}>
              <div>
                <strong>{alert.ruleCode}</strong>
                <p>{alert.message}</p>
                <small>{alert.machineId} · {new Date(alert.startTime).toLocaleString()}</small>
              </div>
              <div className="alert-card__actions">
                <span>{alert.isAcknowledged ? 'Acknowledged' : 'Open'}</span>
                {!alert.isAcknowledged ? <button type="button" onClick={() => onAcknowledgeAlert(alert.id, 'acknowledged from dashboard')}>Acknowledge</button> : null}
              </div>
            </article>
          ))}
        </div>
      </section>
    </>
  );
}
