import { useEffect, useState } from 'react';
import type { AdminMachine, RuleDefinition, Zone } from '../../lib/types';

type Props = {
  rules: RuleDefinition[];
  machines: AdminMachine[];
  zones: Zone[];
  canEditRules: boolean;
  onSaveRule: (rule: RuleDefinition) => Promise<RuleDefinition>;
  onSaveMachine: (machine: AdminMachine) => Promise<AdminMachine>;
  onSaveZone: (zone: Zone) => Promise<Zone>;
};

const severities = ['Info', 'Warning', 'Critical'];

export function AdminPanel({ rules, machines, zones, canEditRules, onSaveRule, onSaveMachine, onSaveZone }: Props) {
  const [ruleDrafts, setRuleDrafts] = useState(rules);
  const [machineDrafts, setMachineDrafts] = useState(machines);
  const [zoneDrafts, setZoneDrafts] = useState(zones);
  const [saved, setSaved] = useState<string | null>(null);

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
    const result = await onSaveRule(rule);
    setRuleDrafts(current => current.map(item => item.id === result.id ? result : item));
    setSaved(`Rule "${result.name}" saved.`);
  };

  const handleSaveMachine = async (machine: AdminMachine) => {
    const result = await onSaveMachine(machine);
    setMachineDrafts(current => current.map(item => item.machineId === result.machineId ? result : item));
    setSaved(`Machine "${result.name}" saved.`);
  };

  const handleSaveZone = async (zone: Zone) => {
    const result = await onSaveZone(zone);
    setZoneDrafts(current => current.map(item => item.zoneId === result.zoneId ? result : item));
    setSaved(`Zone "${result.name}" saved.`);
  };

  return (
    <div className="admin-panel">
      <div className="page-head">
        <div>
          <p className="eyebrow">Administration</p>
          <h1>Equipment &amp; rules</h1>
          <p>Thresholds, machine catalogue and zone definitions. Changes propagate to connected clients over SignalR.</p>
        </div>
        {saved ? <span className="toast">{saved}</span> : null}
      </div>

      {canEditRules ? (
        <section className="panel">
          <div className="panel-header">
            <h2>Alert rules</h2>
            <span>{ruleDrafts.filter(rule => rule.isEnabled).length} of {ruleDrafts.length} enabled</span>
          </div>
          {ruleDrafts.length === 0 ? (
            <div className="empty-state">No rules defined.</div>
          ) : (
            <div className="admin-grid">
              {ruleDrafts.map(rule => (
                <article className="admin-card" key={rule.id}>
                  <div className="admin-card__head">
                    <strong>{rule.name}</strong>
                    <span className="rule-code">{rule.code}</span>
                  </div>

                  <label className="field">
                    <span className="field__label">Name</span>
                    <input value={rule.name} onChange={event => updateRule(rule.id, 'name', event.target.value)} />
                  </label>

                  <div className="admin-card__row">
                    <label className="field">
                      <span className="field__label">Code</span>
                      <input value={rule.code} onChange={event => updateRule(rule.id, 'code', event.target.value)} />
                    </label>
                    <label className="field">
                      <span className="field__label">Severity</span>
                      <select value={rule.severity} onChange={event => updateRule(rule.id, 'severity', event.target.value)}>
                        {severities.map(item => <option key={item} value={item}>{item}</option>)}
                      </select>
                    </label>
                  </div>

                  <div className="admin-card__row">
                    <label className="field">
                      <span className="field__label">Target type</span>
                      <select value={rule.targetType} onChange={event => updateRule(rule.id, 'targetType', event.target.value)}>
                        <option value="Machine">Machine</option>
                        <option value="Zone">Zone</option>
                      </select>
                    </label>
                    <label className="field">
                      <span className="field__label">Target</span>
                      <select value={rule.targetId ?? ''} onChange={event => updateRule(rule.id, 'targetId', event.target.value || null)}>
                        <option value="">— none —</option>
                        {(rule.targetType === 'Zone' ? zoneDrafts.map(zone => ({ id: zone.zoneId, name: zone.name })) : machineDrafts.map(machine => ({ id: machine.machineId, name: machine.name })))
                          .map(option => <option key={option.id} value={option.id}>{option.name}</option>)}
                      </select>
                    </label>
                  </div>

                  <div className="admin-card__row">
                    <label className="field">
                      <span className="field__label">Temperature (°C)</span>
                      <input type="number" value={rule.temperatureThreshold} onChange={event => updateRule(rule.id, 'temperatureThreshold', Number(event.target.value))} />
                    </label>
                    <label className="field">
                      <span className="field__label">Vibration (m/s²)</span>
                      <input type="number" value={rule.vibrationThreshold} onChange={event => updateRule(rule.id, 'vibrationThreshold', Number(event.target.value))} />
                    </label>
                  </div>

                  <div className="admin-card__row">
                    <label className="field">
                      <span className="field__label">Duration (s)</span>
                      <input type="number" value={rule.durationSeconds} onChange={event => updateRule(rule.id, 'durationSeconds', Number(event.target.value))} />
                    </label>
                    <label className="field">
                      <span className="field__label">Cooldown (s)</span>
                      <input type="number" value={rule.cooldownSeconds} onChange={event => updateRule(rule.id, 'cooldownSeconds', Number(event.target.value))} />
                    </label>
                  </div>

                  <div className="admin-card__foot">
                    <label className="switch-row">
                      <input type="checkbox" checked={rule.isEnabled} onChange={event => updateRule(rule.id, 'isEnabled', event.target.checked)} />
                      <span>Enabled</span>
                    </label>
                    <button type="button" className="btn btn--sm" onClick={() => void handleSaveRule(rule)}>Save</button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      ) : null}

      <section className="panel">
        <div className="panel-header">
          <h2>Machines</h2>
          <span>{machineDrafts.length} configured</span>
        </div>
        <div className="admin-grid">
          {machineDrafts.map(machine => (
            <article className="admin-card" key={machine.machineId}>
              <div className="admin-card__head">
                <strong>{machine.name}</strong>
                <span className="rule-code">{machine.machineId}</span>
              </div>

              <label className="field">
                <span className="field__label">Name</span>
                <input value={machine.name} onChange={event => updateMachine(machine.machineId, 'name', event.target.value)} />
              </label>

              <div className="admin-card__row">
                <label className="field">
                  <span className="field__label">Zone</span>
                  <select value={machine.zoneId} onChange={event => updateMachine(machine.machineId, 'zoneId', event.target.value)}>
                    {zoneDrafts.map(zone => <option key={zone.zoneId} value={zone.zoneId}>{zone.name}</option>)}
                  </select>
                </label>
                <label className="field">
                  <span className="field__label">Severity</span>
                  <select value={machine.severity} onChange={event => updateMachine(machine.machineId, 'severity', event.target.value)}>
                    {severities.map(item => <option key={item} value={item}>{item}</option>)}
                  </select>
                </label>
              </div>

              <div className="admin-card__row">
                <label className="field">
                  <span className="field__label">Position X (%)</span>
                  <input type="number" value={machine.locationX} onChange={event => updateMachine(machine.machineId, 'locationX', Number(event.target.value))} />
                </label>
                <label className="field">
                  <span className="field__label">Position Y (%)</span>
                  <input type="number" value={machine.locationY} onChange={event => updateMachine(machine.machineId, 'locationY', Number(event.target.value))} />
                </label>
              </div>

              <div className="admin-card__foot">
                <label className="switch-row">
                  <input type="checkbox" checked={machine.isEnabled} onChange={event => updateMachine(machine.machineId, 'isEnabled', event.target.checked)} />
                  <span>Enabled</span>
                </label>
                <button type="button" className="btn btn--sm" onClick={() => void handleSaveMachine(machine)}>Save</button>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2>Zones</h2>
          <span>{zoneDrafts.length} zones</span>
        </div>
        <div className="admin-grid">
          {zoneDrafts.map(zone => (
            <article className="admin-card" key={zone.zoneId}>
              <div className="admin-card__head">
                <strong>{zone.name}</strong>
                <span className="rule-code">{zone.zoneId}</span>
              </div>

              <label className="field">
                <span className="field__label">Name</span>
                <input value={zone.name} onChange={event => updateZone(zone.zoneId, 'name', event.target.value)} />
              </label>

              <label className="field">
                <span className="field__label">Description</span>
                <input value={zone.description} onChange={event => updateZone(zone.zoneId, 'description', event.target.value)} />
              </label>

              <label className="field">
                <span className="field__label">Colour</span>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <span className="swatch" style={{ background: zone.color }} />
                  <input value={zone.color} onChange={event => updateZone(zone.zoneId, 'color', event.target.value)} />
                </div>
              </label>

              <div className="admin-card__foot">
                <label className="switch-row">
                  <input type="checkbox" checked={zone.isActive} onChange={event => updateZone(zone.zoneId, 'isActive', event.target.checked)} />
                  <span>Active</span>
                </label>
                <button type="button" className="btn btn--sm" onClick={() => void handleSaveZone(zone)}>Save</button>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
