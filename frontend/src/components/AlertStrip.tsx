import type { Alert } from '../lib/types';

type Props = {
  alerts: Alert[];
};

export function AlertStrip({ alerts }: Props) {
  const topAlert = alerts.find(alert => !alert.isAcknowledged) ?? alerts[0];

  if (!topAlert) {
    return (
      <div className="alert-strip">
        <span className="alert-strip__icon" />
        <span className="alert-strip__text">All clear — no active alerts.</span>
      </div>
    );
  }

  return (
    <div className={`alert-strip ${topAlert.severity.toLowerCase()}`}>
      <span className="alert-strip__icon" />
      <span className="rule-code">{topAlert.ruleCode}</span>
      <span className="alert-strip__text">{topAlert.message}</span>
      <span className="alert-strip__time">{new Date(topAlert.startTime).toLocaleTimeString()}</span>
    </div>
  );
}
