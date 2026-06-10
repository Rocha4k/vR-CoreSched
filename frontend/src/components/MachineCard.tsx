import type { MachineState } from '../lib/types';

type Props = {
  machine: MachineState;
};

export function MachineCard({ machine }: Props) {
  return (
    <article className={`machine-card ${machine.severity.toLowerCase()}`}>
      <div className="machine-card__header">
        <h3>{machine.name}</h3>
        <span>{machine.zone}</span>
      </div>
      <div className="machine-card__metrics">
        <div><strong>{machine.temperatureC.toFixed(1)}°C</strong><span>Temperatura</span></div>
        <div><strong>{machine.vibrationMs2.toFixed(1)}</strong><span>Vibração</span></div>
        <div><strong>{machine.rpm}</strong><span>RPM</span></div>
        <div><strong>{machine.energyKwh.toFixed(1)} kWh</strong><span>Energia</span></div>
      </div>
    </article>
  );
}
