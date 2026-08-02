import { useMemo, useRef, useState } from 'react';
import type { PointerEvent as ReactPointerEvent } from 'react';
import type { AdminMachine, FloorplanLayout, FloorplanPin, FloorplanPoint, LightingDevice } from '../lib/types';

type Props = {
  layout: FloorplanLayout;
  machines: AdminMachine[];
  lighting: LightingDevice[];
  editable?: boolean;
  onMovePin?: (pin: FloorplanPin) => void;
  onAddBoundaryPoint?: (point: FloorplanPoint) => void;
};

function parsePoints(json: string): FloorplanPoint[] {
  try {
    const parsed = JSON.parse(json) as FloorplanPoint[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function toPointString(points: FloorplanPoint[]) {
  return points.map(point => `${point.x},${point.y}`).join(' ');
}

function clamp(value: number) {
  return Math.max(0, Math.min(100, value));
}

export function WarehouseMap({ layout, machines, lighting, editable = false, onMovePin, onAddBoundaryPoint }: Props) {
  const svgRef = useRef<SVGSVGElement | null>(null);
  const [draggingPinId, setDraggingPinId] = useState<number | null>(null);
  const [previewPin, setPreviewPin] = useState<FloorplanPin | null>(null);

  const boundaryPoints = useMemo(() => parsePoints(layout.boundaryPointsJson), [layout.boundaryPointsJson]);
  const machineById = useMemo(() => new Map(machines.map(machine => [machine.machineId, machine])), [machines]);
  const lightingById = useMemo(() => new Map(lighting.map(item => [item.id, item])), [lighting]);

  const pointerToPoint = (clientX: number, clientY: number) => {
    const svg = svgRef.current;
    if (!svg) {
      return { x: 0, y: 0 };
    }

    const box = svg.getBoundingClientRect();
    const x = clamp(((clientX - box.left) / box.width) * 100);
    const y = clamp(((clientY - box.top) / box.height) * 100);
    return { x, y };
  };

  const handleCanvasPointerDown = (event: ReactPointerEvent<SVGRectElement>) => {
    if (!editable || !onAddBoundaryPoint) {
      return;
    }

    const point = pointerToPoint(event.clientX, event.clientY);
    onAddBoundaryPoint(point);
  };

  const handlePinPointerDown = (event: ReactPointerEvent<SVGGElement>, pin: FloorplanPin) => {
    if (!editable || !onMovePin) {
      return;
    }

    event.preventDefault();
    svgRef.current?.setPointerCapture(event.pointerId);
    setDraggingPinId(pin.id);
    setPreviewPin(pin);
  };

  const handlePointerMove = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (draggingPinId === null || previewPin === null) {
      return;
    }

    const point = pointerToPoint(event.clientX, event.clientY);
    setPreviewPin({ ...previewPin, x: point.x, y: point.y });
  };

  const handlePointerUp = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (draggingPinId === null || previewPin === null) {
      return;
    }

    onMovePin?.(previewPin);
    setDraggingPinId(null);
    setPreviewPin(null);

    try {
      svgRef.current?.releasePointerCapture(event.pointerId);
    } catch {
    }
  };

  const pointsForRender = boundaryPoints.length > 0 ? toPointString(boundaryPoints) : '8,14 92,14 96,26 96,86 8,86 8,24';

  return (
    <div className={`warehouse-map texture-${layout.textureKey}`}>
      <svg
        ref={svgRef}
        viewBox="0 0 100 100"
        role="img"
        aria-label="Warehouse floor plan"
        className="warehouse-map__svg"
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
      >
        <defs>
          <pattern id="warehouse-grid" width="5" height="5" patternUnits="userSpaceOnUse">
            <path d="M 5 0 L 0 0 0 5" fill="none" stroke="rgba(255,255,255,0.05)" strokeWidth="0.3" />
          </pattern>
          <radialGradient id="warehouse-vignette" cx="50%" cy="40%" r="70%">
            <stop offset="0%" stopColor="rgba(255,255,255,0.035)" />
            <stop offset="100%" stopColor="rgba(0,0,0,0)" />
          </radialGradient>
        </defs>

        <rect x="0" y="0" width="100" height="100" className="warehouse-map__background" />
        <rect x="0" y="0" width="100" height="100" fill="url(#warehouse-grid)" />
        <rect x="0" y="0" width="100" height="100" fill="url(#warehouse-vignette)" />
        <polygon points={pointsForRender} className="warehouse-map__boundary" />

        {editable && onAddBoundaryPoint ? (
          <rect
            x="0"
            y="0"
            width="100"
            height="100"
            fill="transparent"
            className="warehouse-map__canvas-hitbox"
            onPointerDown={handleCanvasPointerDown}
          />
        ) : null}

        {layout.pins.filter(pin => pin.isVisible).map(pin => {
          const activePin = draggingPinId === pin.id && previewPin ? previewPin : pin;
          const machine = machineById.get(pin.deviceId);
          const light = lightingById.get(pin.deviceId);
          const statusClass = pin.deviceType === 'Light'
            ? (light?.isOn ? 'pin--active' : 'pin--off')
            : (machine?.severity?.toLowerCase() ?? 'pin--neutral');

          return (
            <g
              key={pin.id}
              transform={`translate(${activePin.x}, ${activePin.y})`}
              className={`floorplan-pin ${statusClass}`}
              onPointerDown={event => handlePinPointerDown(event, pin)}
            >
              {pin.deviceType === 'Light' ? <circle r="2.2" /> : <rect x="-2.5" y="-2.5" width="5" height="5" rx="0.8" />}
              <text x="3" y="-2.4">{pin.label}</text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
