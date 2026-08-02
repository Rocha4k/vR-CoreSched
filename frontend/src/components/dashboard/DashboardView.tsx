import { AlertStrip } from '../AlertStrip';
import { MachineCard } from '../MachineCard';
import { WarehouseMap } from '../WarehouseMap';
import { ConsumptionChart } from '../analytics/ConsumptionChart';
import { MaintenancePanel } from '../maintenance/MaintenancePanel';
import type { Alert, LightingDevice, WorkspaceSnapshot } from '../../lib/types';

type Props = {
  workspace: WorkspaceSnapshot;
  onToggleLight: (deviceId: string) => void;
  onAcknowledgeAlert: (alertId: string, note: string) => void;
  onCreateMaintenance: (machineId: string, title: string, notes: string, status: string) => Promise<void>;
  canCreateMaintenance: boolean;
};

function BulbIcon({ isOn }: { isOn: boolean }) {
  return (
    <svg className="lamp-bulb-svg" viewBox="0 0 48 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      {isOn && <ellipse cx="24" cy="26" rx="18" ry="18" fill="rgba(240,196,112,0.14)" />}
      <path
        d="M24 6C15.163 6 8 13.163 8 22c0 5.44 2.66 10.26 6.77 13.27L16 40h16l1.23-4.73C37.34 32.26 40 27.44 40 22c0-8.837-7.163-16-16-16z"
        fill={isOn ? '#f0c470' : '#2a2b30'}
        stroke={isOn ? '#c9a05a' : '#3d3e45'}
        strokeWidth="1.5"
      />
      {isOn && <path d="M19 14c-3.5 2-6 5.8-6 10" stroke="rgba(255,255,255,0.5)" strokeWidth="2.5" strokeLinecap="round" />}
      <path d="M20 34v2M28 34v2" stroke={isOn ? '#c9a05a' : '#3d3e45'} strokeWidth="1.5" strokeLinecap="round" />
      <rect x="17" y="40" width="14" height="4" rx="1.5" fill={isOn ? '#9c7c45' : '#2a2b30'} stroke={isOn ? '#7d6335' : '#3d3e45'} strokeWidth="1" />
      <rect x="17.5" y="44" width="13" height="4" rx="1.5" fill={isOn ? '#7d6335' : '#212226'} stroke={isOn ? '#63502c' : '#33343a'} strokeWidth="1" />
      <rect x="19" y="48" width="10" height="3" rx="1.5" fill={isOn ? '#63502c' : '#1b1c20'} stroke={isOn ? '#4d3e22' : '#2a2b30'} strokeWidth="1" />
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
      title={`${light.name} — last change by ${light.lastCommandSource}`}
    >
      <span className="lamp-card__glow" aria-hidden="true" />
      <span className="lamp-card__icon">
        <BulbIcon isOn={light.isOn} />
      </span>
      <span className="lamp-card__info">
        <span className="lamp-card__name">{light.name}</span>
        <span className="lamp-card__zone">{light.zone}</span>
        <span className="lamp-card__status">{light.isOn ? 'On' : 'Off'}</span>
      </span>
    </button>
  );
}

function severityBadge(severity: string) {
  const key = severity.toLowerCase();
  if (key === 'critical') return 'badge badge--critical';
  if (key === 'warning') return 'badge badge--warning';
  return 'badge badge--info';
}

function AlertRow({ alert, onAcknowledge }: { alert: Alert; onAcknowledge: (id: string, note: string) => void }) {
  return (
    <article className={`list-card alert-card ${alert.severity.toLowerCase()}`}>
      <div className="list-card__head">
        <div>
          <span className={severityBadge(alert.severity)}>{alert.severity}</span>{' '}
          <span className="rule-code">{alert.ruleCode}</span>
        </div>
        <div className="list-card__actions">
          {alert.isAcknowledged ? (
            <span className="badge badge--ok">Acknowledged</span>
          ) : (
            <button type="button" className="btn btn--sm" onClick={() => onAcknowledge(alert.id, 'Acknowledged from dashboard')}>
              Acknowledge
            </button>
          )}
        </div>
      </div>
      <p className="list-card__body">{alert.message}</p>
      <div className="list-card__meta">
        <span>{alert.machineId}</span>
        <span className="dot">·</span>
        <span>{new Date(alert.startTime).toLocaleString()}</span>
      </div>
    </article>
  );
}

export function DashboardView({ workspace, onToggleLight, onAcknowledgeAlert, onCreateMaintenance, canCreateMaintenance }: Props) {
  const { dashboard, floorplan, machines, zones } = workspace;

  const totalEnergy = dashboard.aggregates.reduce((sum, item) => sum + item.totalKwh, 0);
  const totalCost = dashboard.aggregates.reduce((sum, item) => sum + item.costEuro, 0);
  const onlineMachines = dashboard.machines.filter(machine => machine.isOnline).length;
  const lightsOn = dashboard.lighting.filter(light => light.isOn).length;
  const openAlerts = dashboard.alerts.filter(alert => !alert.isAcknowledged).length;

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Live floor status</h1>
          <p>Telemetry, alerting and lighting control across {zones.length} zones and {floorplan.pins.length} mapped points.</p>
        </div>
      </div>

      <div className="stat-row">
        <div className="stat">
          <div className="stat__label">Machines online</div>
          <div className="stat__value">{onlineMachines}<span className="stat__unit">/ {dashboard.machines.length}</span></div>
        </div>
        <div className="stat">
          <div className="stat__label">Open alerts</div>
          <div className="stat__value">{openAlerts}</div>
          <div className="stat__hint">{dashboard.alerts.length} in the last window</div>
        </div>
        <div className="stat">
          <div className="stat__label">Aggregated energy</div>
          <div className="stat__value">{totalEnergy.toFixed(1)}<span className="stat__unit">kWh</span></div>
        </div>
        <div className="stat">
          <div className="stat__label">Estimated cost</div>
          <div className="stat__value">{totalCost.toFixed(2)}<span className="stat__unit">€</span></div>
        </div>
      </div>

      <AlertStrip alerts={dashboard.alerts} />

      <section className="dashboard-grid">
        <div className="panel">
          <div className="panel-header">
            <h2>Floor plan</h2>
            <span>{lightsOn}/{dashboard.lighting.length} lights on</span>
          </div>
          <WarehouseMap layout={floorplan} machines={machines} lighting={dashboard.lighting} />

          <div className="lighting-panel">
            <div className="lighting-panel__header">
              <div className="lighting-panel__title">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M12 2a7 7 0 0 1 5 11.9V17H7v-3.1A7 7 0 0 1 12 2z" fill="currentColor" opacity="0.85" />
                  <path d="M9 17h6M9.5 19.5h5M10 22h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
                </svg>
                <span>Lighting</span>
              </div>
              <span className="badge badge--muted">Click to toggle</span>
            </div>
            <div className="lamp-grid">
              {dashboard.lighting.map(light => (
                <LampCard key={light.id} light={light} onToggle={() => onToggleLight(light.id)} />
              ))}
            </div>
          </div>
        </div>

        <div className="panel">
          <div className="panel-header">
            <h2>Machines</h2>
            <span>{dashboard.machines.length} assets</span>
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

      <section className="panel" style={{ marginTop: 16 }}>
        <div className="panel-header">
          <h2>Alerts</h2>
          <span>{openAlerts} open · {dashboard.alerts.length} total</span>
        </div>
        {dashboard.alerts.length === 0 ? (
          <div className="empty-state">No alerts recorded yet.</div>
        ) : (
          <div className="list scroll-list">
            {dashboard.alerts.map(alert => (
              <AlertRow key={alert.id} alert={alert} onAcknowledge={onAcknowledgeAlert} />
            ))}
          </div>
        )}
      </section>
    </>
  );
}
