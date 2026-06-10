import type { AdminMachine, MaintenanceRecord } from '../../lib/types';

type Props = {
  maintenanceRecords: MaintenanceRecord[];
  machines: AdminMachine[];
  canCreate: boolean;
  onCreateMaintenance: (machineId: string, title: string, notes: string, status: string) => Promise<void>;
};

export function MaintenancePanel({ maintenanceRecords, machines, canCreate, onCreateMaintenance }: Props) {
  const sortedRecords = [...maintenanceRecords].sort((left, right) => right.createdAt.localeCompare(left.createdAt));

  return (
    <section className="panel maintenance-panel">
      <div className="panel-header">
        <h2>Histórico de manutenção</h2>
        <span>{maintenanceRecords.length} registos</span>
      </div>

      <form
        className="maintenance-panel__form"
        onSubmit={event => {
          event.preventDefault();
          if (!canCreate) return;

          const form = event.currentTarget;
          const formData = new FormData(form);
          const machineId = String(formData.get('machineId') ?? machines[0]?.machineId ?? '');
          const title = String(formData.get('title') ?? '').trim();
          const notes = String(formData.get('notes') ?? '').trim();
          const status = String(formData.get('status') ?? 'Open');

          if (!machineId || !title) {
            return;
          }

          void onCreateMaintenance(machineId, title, notes, status).then(() => form.reset());
        }}
      >
        <select name="machineId" defaultValue={machines[0]?.machineId ?? ''} disabled={!canCreate}>
          {machines.map(machine => <option key={machine.machineId} value={machine.machineId}>{machine.name}</option>)}
        </select>
        <input name="title" placeholder="Título da manutenção" disabled={!canCreate} />
        <input name="notes" placeholder="Notas e observações" disabled={!canCreate} />
        <select name="status" defaultValue="Open" disabled={!canCreate}>
          <option value="Open">Open</option>
          <option value="InProgress">InProgress</option>
          <option value="Closed">Closed</option>
        </select>
        <button type="submit" disabled={!canCreate}>Registar manutenção</button>
      </form>

      <div className="maintenance-panel__list">
        {sortedRecords.map(record => (
          <article key={record.id} className="maintenance-card">
            <div className="maintenance-card__head">
              <strong>{record.title}</strong>
              <span>{record.status}</span>
            </div>
            <p>{record.notes}</p>
            <small>{record.machineId} · {record.createdBy} · {new Date(record.createdAt).toLocaleString()}</small>
          </article>
        ))}
      </div>
    </section>
  );
}
