import { WarehouseMap } from '../WarehouseMap';
import type { AdminMachine, FloorplanLayout, FloorplanPin, FloorplanPoint, LightingDevice } from '../../lib/types';

type Props = {
  layout: FloorplanLayout;
  machines: AdminMachine[];
  lighting: LightingDevice[];
  onMovePin: (pin: FloorplanPin) => void;
  onAddBoundaryPoint: (point: FloorplanPoint) => void;
};

export function FloorplanEditor({ layout, machines, lighting, onMovePin, onAddBoundaryPoint }: Props) {
  const pinCount = layout.pins.length;
  const machinePins = layout.pins.filter(pin => pin.deviceType === 'Machine').length;
  const lightPins = layout.pins.filter(pin => pin.deviceType === 'Light').length;

  return (
    <div className="floorplan-editor">
      <section className="panel floorplan-editor__canvas-panel">
        <div className="panel-header">
          <h2>Planta do armazém</h2>
          <span>clica no fundo para acrescentar pontos ao contorno e arrasta os hotspots para reposicionar.</span>
        </div>
        <WarehouseMap
          layout={layout}
          machines={machines}
          lighting={lighting}
          editable
          onMovePin={onMovePin}
          onAddBoundaryPoint={onAddBoundaryPoint}
        />
      </section>

      <aside className="panel floorplan-editor__side">
        <div className="panel-header">
          <h2>Mapa</h2>
          <span>{layout.textureKey}</span>
        </div>
        <div className="metric-stack">
          <div><strong>{pinCount}</strong><span>pontos mapeados</span></div>
          <div><strong>{machinePins}</strong><span>máquinas</span></div>
          <div><strong>{lightPins}</strong><span>luzes</span></div>
        </div>
        <p className="editor-note">A textura é única por armazém, mas o padrão visual é o mesmo em todos os layouts, o que facilita replicar a planta e mudar apenas a geometria e os hotspots.</p>
        <div className="pin-legend">
          {layout.pins.map(pin => (
            <div key={pin.id} className="pin-legend__item">
              <strong>{pin.label}</strong>
              <span>{pin.deviceType} · {pin.zoneId}</span>
            </div>
          ))}
        </div>
      </aside>
    </div>
  );
}
