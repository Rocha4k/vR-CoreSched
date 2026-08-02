import type { AdminMachine, MaintenanceRecord } from '../../lib/types';

type Props = {
  maintenanceRecords: MaintenanceRecord[];
  machines: AdminMachine[];
  canCreate: boolean;
  onCreateMaintenance: (machineId: string, title: string, notes: string, status: string) => Promise<void>;
};

function statusTone(status: string) {
  const key = status.toLowerCase();
  if (key === 'closed') return 'badge badge--ok';
  if (key === 'inprogress') return 'badge badge--warning';
  return 'badge badge--info';
}

export function MaintenancePanel({ maintenanceRecords, machines, canCreate, onCreateMaintenance }: Props) {
  const sortedRecords = [...maintenanceRecords].sort((left, right) => right.createdAt.localeCompare(left.createdAt));

  return (
    <section className="panel">
      <div className="panel-header">
        <h2>Maintenance</h2>
        <span>{maintenanceRecords.length} records</span>
      </div>

      <form
        className="report-panel__filters"
        style={{ marginBottom: 14 }}
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
        <label className="field">
          <span className="field__label">Machine</span>
          <select name="machineId" defaultValue={machines[0]?.machineId ?? ''} disabled={!canCreate}>
            {machines.map(machine => <option key={machine.machineId} value={machine.machineId}>{machine.name}</option>)}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Title</span>
          <input name="title" placeholder="e.g. Replace bearing" disabled={!canCreate} />
        </label>
        <label className="field">
          <span className="field__label">Status</span>
          <select name="status" defaultValue="Open" disabled={!canCreate}>
            <option value="Open">Open</option>
            <option value="InProgress">In progress</option>
            <option value="Closed">Closed</option>
          </select>
        </label>
        <button type="submit" className="btn btn--primary" disabled={!canCreate}>Log</button>
        <label className="field" style={{ gridColumn: '1 / -1' }}>
          <span className="field__label">Notes</span>
          <input name="notes" placeholder="Observations" disabled={!canCreate} />
        </label>
      </form>

      {!canCreate ? <p className="inline-note">Supervisor or admin role required to log maintenance.</p> : null}

      {sortedRecords.length === 0 ? (
        <div className="empty-state">No maintenance recorded yet.</div>
      ) : (
        <div className="list scroll-list">
          {sortedRecords.map(record => (
            <article key={record.id} className="list-card">
              <div className="list-card__head">
                <span className="list-card__title">{record.title}</span>
                <span className={statusTone(record.status)}>{record.status}</span>
              </div>
              {record.notes ? <p className="list-card__body">{record.notes}</p> : null}
              <div className="list-card__meta">
                <span>{record.machineId}</span>
                <span className="dot">·</span>
                <span>{record.createdBy}</span>
                <span className="dot">·</span>
                <span>{new Date(record.createdAt).toLocaleString()}</span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
