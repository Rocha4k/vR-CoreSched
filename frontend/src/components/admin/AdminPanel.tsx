import { useEffect, useState } from 'react';
import type { AdminMachine, RuleDefinition, Zone } from '../../lib/types';

type Props = {
  rules: RuleDefinition[];
  machines: AdminMachine[];
  zones: Zone[];
  onSaveRule: (rule: RuleDefinition) => Promise<RuleDefinition>;
  onSaveMachine: (machine: AdminMachine) => Promise<AdminMachine>;
  onSaveZone: (zone: Zone) => Promise<Zone>;
};

export function AdminPanel({ rules, machines, zones, onSaveRule, onSaveMachine, onSaveZone }: Props) {
  const [ruleDrafts, setRuleDrafts] = useState(rules);
  const [machineDrafts, setMachineDrafts] = useState(machines);
  const [zoneDrafts, setZoneDrafts] = useState(zones);

  useEffect(() => setRuleDrafts(rules), [rules]);
  useEffect(() => setMachineDrafts(machines), [machines]);
  useEffect(() => setZoneDrafts(zones), [zones]);

  const updateRule = (ruleId: string, field: keyof RuleDefinition, value: string | number | boolean | null) => {
    setRuleDrafts(current => current.map(rule => rule.id === ruleId ? { ...rule, [field]: value } as RuleDefinition : rule));
  };

  const updateMachine = (machineId: string, field: keyof AdminMachine, value: string | number | boolean | null) => {
    setMachineDrafts(current => current.map(machine => machine.machineId === machineId ? { ...machine, [field]: value } as AdminMachine : machine));
  };

  const updateZone = (zoneId: string, field: keyof Zone, value: string | boolean) => {
    setZoneDrafts(current => current.map(zone => zone.zoneId === zoneId ? { ...zone, [field]: value } as Zone : zone));
  };

  const handleSaveRule = async (rule: RuleDefinition) => {
    const saved = await onSaveRule(rule);
    setRuleDrafts(current => current.map(item => item.id === saved.id ? saved : item));
  };

  const handleSaveMachine = async (machine: AdminMachine) => {
    const saved = await onSaveMachine(machine);
    setMachineDrafts(current => current.map(item => item.machineId === saved.machineId ? saved : item));
  };

  const handleSaveZone = async (zone: Zone) => {
    const saved = await onSaveZone(zone);
    setZoneDrafts(current => current.map(item => item.zoneId === saved.zoneId ? saved : item));
  };

  return (
    <div className="admin-panel">
      <section className="panel admin-section">
        <div className="panel-header">
          <h2>Regras</h2>
          <span>{ruleDrafts.length} ativas</span>
        </div>
        <div className="admin-grid">
          {ruleDrafts.map(rule => (
            <article className="admin-card" key={rule.id}>
              <input value={rule.name} onChange={event => updateRule(rule.id, 'name', event.target.value)} />
              <div className="admin-card__row">
                <input value={rule.code} onChange={event => updateRule(rule.id, 'code', event.target.value)} />
                <select value={rule.severity} onChange={event => updateRule(rule.id, 'severity', event.target.value)}>
                  <option value="Info">Info</option>
                  <option value="Warning">Warning</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              <div className="admin-card__row">
                <input value={rule.targetType} onChange={event => updateRule(rule.id, 'targetType', event.target.value)} />
                <input value={rule.targetId ?? ''} onChange={event => updateRule(rule.id, 'targetId', event.target.value || null)} />
              </div>
              <div className="admin-card__row">
                <label><span>T</span><input type="number" value={rule.temperatureThreshold} onChange={event => updateRule(rule.id, 'temperatureThreshold', Number(event.target.value))} /></label>
                <label><span>V</span><input type="number" value={rule.vibrationThreshold} onChange={event => updateRule(rule.id, 'vibrationThreshold', Number(event.target.value))} /></label>
              </div>
              <div className="admin-card__row">
                <label><span>Duração</span><input type="number" value={rule.durationSeconds} onChange={event => updateRule(rule.id, 'durationSeconds', Number(event.target.value))} /></label>
                <label><span>Cooldown</span><input type="number" value={rule.cooldownSeconds} onChange={event => updateRule(rule.id, 'cooldownSeconds', Number(event.target.value))} /></label>
              </div>
              <label className="switch-row">
                <input type="checkbox" checked={rule.isEnabled} onChange={event => updateRule(rule.id, 'isEnabled', event.target.checked)} />
                <span>Regra ativa</span>
              </label>
              <button type="button" onClick={() => void handleSaveRule(rule)}>Guardar regra</button>
            </article>
          ))}
        </div>
      </section>

      <section className="panel admin-section">
        <div className="panel-header">
          <h2>Máquinas</h2>
          <span>{machineDrafts.length} configuradas</span>
        </div>
        <div className="admin-grid admin-grid--wide">
          {machineDrafts.map(machine => (
            <article className="admin-card" key={machine.machineId}>
              <input value={machine.name} onChange={event => updateMachine(machine.machineId, 'name', event.target.value)} />
              <div className="admin-card__row">
                <input value={machine.zoneId} onChange={event => updateMachine(machine.machineId, 'zoneId', event.target.value)} />
                <select value={machine.severity} onChange={event => updateMachine(machine.machineId, 'severity', event.target.value)}>
                  <option value="Info">Info</option>
                  <option value="Warning">Warning</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              <div className="admin-card__row">
                <label><span>X</span><input type="number" value={machine.locationX} onChange={event => updateMachine(machine.machineId, 'locationX', Number(event.target.value))} /></label>
                <label><span>Y</span><input type="number" value={machine.locationY} onChange={event => updateMachine(machine.machineId, 'locationY', Number(event.target.value))} /></label>
              </div>
              <label className="switch-row">
                <input type="checkbox" checked={machine.isEnabled} onChange={event => updateMachine(machine.machineId, 'isEnabled', event.target.checked)} />
                <span>Máquina ativa</span>
              </label>
              <button type="button" onClick={() => void handleSaveMachine(machine)}>Guardar máquina</button>
            </article>
          ))}
        </div>
      </section>

      <section className="panel admin-section">
        <div className="panel-header">
          <h2>Zonas</h2>
          <span>{zoneDrafts.length} zonas</span>
        </div>
        <div className="admin-grid admin-grid--wide">
          {zoneDrafts.map(zone => (
            <article className="admin-card" key={zone.zoneId}>
              <input value={zone.name} onChange={event => updateZone(zone.zoneId, 'name', event.target.value)} />
              <input value={zone.description} onChange={event => updateZone(zone.zoneId, 'description', event.target.value)} />
              <div className="admin-card__row">
                <input value={zone.color} onChange={event => updateZone(zone.zoneId, 'color', event.target.value)} />
                <label className="switch-row">
                  <input type="checkbox" checked={zone.isActive} onChange={event => updateZone(zone.zoneId, 'isActive', event.target.checked)} />
                  <span>Zona ativa</span>
                </label>
              </div>
              <button type="button" onClick={() => void handleSaveZone(zone)}>Guardar zona</button>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
