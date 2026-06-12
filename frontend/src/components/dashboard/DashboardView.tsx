import { AlertStrip } from '../AlertStrip';
import { MachineCard } from '../MachineCard';
import { WarehouseMap } from '../WarehouseMap';
import { ConsumptionChart } from '../analytics/ConsumptionChart';
import { MaintenancePanel } from '../maintenance/MaintenancePanel';
import type { LightingDevice, WorkspaceSnapshot } from '../../lib/types';

type Props = {
  workspace: WorkspaceSnapshot;
  connectionStatus: string;
  onToggleLight: (deviceId: string) => void;
  onAcknowledgeAlert: (alertId: string, note: string) => void;
  onCreateMaintenance: (machineId: string, title: string, notes: string, status: string) => Promise<void>;
  canCreateMaintenance: boolean;
};

function BulbIcon({ isOn }: { isOn: boolean }) {
  return (
    <svg className="lamp-bulb-svg" viewBox="0 0 48 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      {isOn && (
        <>
          <ellipse cx="24" cy="26" rx="20" ry="20" fill="rgba(253,224,71,0.18)" />
          <ellipse cx="24" cy="26" rx="14" ry="14" fill="rgba(253,224,71,0.22)" />
        </>
      )}
      {/* Bulb glass body */}
      <path
        d="M24 6C15.163 6 8 13.163 8 22c0 5.44 2.66 10.26 6.77 13.27L16 40h16l1.23-4.73C37.34 32.26 40 27.44 40 22c0-8.837-7.163-16-16-16z"
        fill={isOn ? '#fde047' : '#334155'}
        stroke={isOn ? '#f59e0b' : '#475569'}
        strokeWidth="1.5"
      />
      {/* Inner shine when on */}
      {isOn && (
        <path
          d="M19 14c-3.5 2-6 5.8-6 10"
          stroke="rgba(255,255,255,0.55)"
          strokeWidth="2.5"
          strokeLinecap="round"
        />
      )}
      {/* Filament lines */}
      <path
        d="M20 34v2M28 34v2"
        stroke={isOn ? '#f59e0b' : '#475569'}
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      {/* Base segments */}
      <rect x="17" y="40" width="14" height="4" rx="1.5" fill={isOn ? '#d97706' : '#334155'} stroke={isOn ? '#b45309' : '#475569'} strokeWidth="1" />
      <rect x="17.5" y="44" width="13" height="4" rx="1.5" fill={isOn ? '#b45309' : '#1e293b'} stroke={isOn ? '#92400e' : '#334155'} strokeWidth="1" />
      <rect x="19" y="48" width="10" height="3" rx="1.5" fill={isOn ? '#92400e' : '#1e293b'} stroke={isOn ? '#78350f' : '#334155'} strokeWidth="1" />
      {/* Thread lines on base */}
      <line x1="17" y1="42.5" x2="31" y2="42.5" stroke={isOn ? '#92400e' : '#0f172a'} strokeWidth="0.8" strokeOpacity="0.6" />
      <line x1="17.5" y1="46" x2="30.5" y2="46" stroke={isOn ? '#78350f' : '#0f172a'} strokeWidth="0.8" strokeOpacity="0.6" />
    </svg>
  );
}

function LampCard({ light, onToggle }: { light: LightingDevice; onToggle: () => void }) {
  return (
    <button
      type="button"
      className={`lamp-card ${light.isOn ? 'lamp-card--on' : 'lamp-card--off'}`}
      onClick={onToggle}
      aria-pressed={light.isOn}
    >
      <div className="lamp-card__glow" aria-hidden="true" />
      <div className="lamp-card__icon">
        <BulbIcon isOn={light.isOn} />
      </div>
      <div className="lamp-card__info">
        <span className="lamp-card__name">{light.name}</span>
        <span className="lamp-card__zone">{light.zone}</span>
        <span className={`lamp-card__status ${light.isOn ? 'lamp-card__status--on' : 'lamp-card__status--off'}`}>
          <span className="lamp-card__dot" aria-hidden="true" />
          {light.isOn ? 'Ligada' : 'Desligada'}
        </span>
      </div>
    </button>
  );
}

export function DashboardView({ workspace, connectionStatus, onToggleLight, onAcknowledgeAlert, onCreateMaintenance, canCreateMaintenance }: Props) {
  const { dashboard, floorplan, machines, zones } = workspace;
  const totalEnergy = dashboard.aggregates.reduce((sum, item) => sum + item.totalKwh, 0);
  const totalCost = dashboard.aggregates.reduce((sum, item) => sum + item.costEuro, 0);
  const onlineMachines = dashboard.machines.filter(machine => machine.isOnline).length;
  const lightsOn = dashboard.lighting.filter(l => l.isOn).length;

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

          <div className="lighting-panel">
            <div className="lighting-panel__header">
              <div className="lighting-panel__title">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M12 2a7 7 0 0 1 5 11.9V17H7v-3.1A7 7 0 0 1 12 2z" fill="currentColor" opacity="0.9"/>
                  <path d="M9 17h6M9.5 19.5h5M10 22h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                </svg>
                <span>Iluminação</span>
              </div>
              <span className="lighting-panel__badge">
                {lightsOn}/{dashboard.lighting.length} ligadas
              </span>
            </div>
            <div className="lamp-grid">
              {dashboard.lighting.map(light => (
                <LampCard key={light.id} light={light} onToggle={() => onToggleLight(light.id)} />
              ))}
            </div>
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
