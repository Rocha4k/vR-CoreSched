import { memo } from 'react';
import type { MachineState } from '../lib/types';

type Props = {
  machine: MachineState;
};

function MachineCardBase({ machine }: Props) {
  const severity = machine.isOnline ? machine.severity.toLowerCase() : 'offline';

  return (
    <article className={`machine-card ${severity}`}>
      <div className="machine-card__header">
        <div className="machine-card__title">
          <h3>{machine.name}</h3>
          <span>{machine.zone}</span>
        </div>
        <span className={`badge badge--${machine.isOnline ? severityTone(machine.severity) : 'muted'}`}>
          {machine.isOnline ? machine.severity : 'Offline'}
        </span>
      </div>
      <div className="machine-card__metrics">
        <div><strong>{machine.temperatureC.toFixed(1)}°</strong><span>Temp</span></div>
        <div><strong>{machine.vibrationMs2.toFixed(1)}</strong><span>Vib m/s²</span></div>
        <div><strong>{machine.rpm}</strong><span>RPM</span></div>
        <div><strong>{machine.energyKwh.toFixed(1)}</strong><span>kWh</span></div>
      </div>
    </article>
  );
}

function severityTone(severity: string) {
  const key = severity.toLowerCase();
  if (key === 'critical') return 'critical';
  if (key === 'warning') return 'warning';
  return 'ok';
}

// Telemetry updates rewrite the machine list on every flush; skip untouched cards.
export const MachineCard = memo(MachineCardBase);
