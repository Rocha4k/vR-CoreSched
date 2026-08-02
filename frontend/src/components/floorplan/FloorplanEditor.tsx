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
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Administration</p>
          <h1>Floor plan editor</h1>
          <p>Click the canvas to append boundary points, drag hotspots to reposition equipment. Changes are saved immediately.</p>
        </div>
        <span className="badge">Updated {new Date(layout.updatedAt).toLocaleString()}</span>
      </div>

      <div className="floorplan-editor">
        <section className="panel">
          <div className="panel-header">
            <h2>{layout.name}</h2>
            <span>{layout.canvasWidth} × {layout.canvasHeight}</span>
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

        <aside className="panel">
          <div className="panel-header">
            <h2>Mapped points</h2>
            <span>{layout.textureKey}</span>
          </div>

          <div className="metric-stack">
            <div><strong>{pinCount}</strong><span>Points</span></div>
            <div><strong>{machinePins}</strong><span>Machines</span></div>
            <div><strong>{lightPins}</strong><span>Lights</span></div>
          </div>

          <p className="editor-note">
            The texture is per warehouse while the visual language stays identical across layouts, so a new site only needs
            its geometry and hotspots re-drawn.
          </p>

          <div className="pin-legend">
            {layout.pins.map(pin => (
              <div key={pin.id} className="pin-legend__item">
                <span className={`pin-legend__dot ${pin.deviceType === 'Light' ? 'pin-legend__dot--light' : ''}`} />
                <strong>{pin.label}</strong>
                <span>{pin.zoneId}</span>
              </div>
            ))}
          </div>
        </aside>
      </div>
    </>
  );
}
