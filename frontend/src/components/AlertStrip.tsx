import type { Alert } from '../lib/types';

type Props = {
  alerts: Alert[];
};

export function AlertStrip({ alerts }: Props) {
  const topAlert = alerts[0];

  if (!topAlert) {
    return <div className="alert-strip calm">Sem alertas críticos neste momento.</div>;
  }

  return (
    <div className={`alert-strip ${topAlert.severity.toLowerCase()}`}>
      <strong>{topAlert.severity}</strong>
      <span>{topAlert.message}</span>
    </div>
  );
}
