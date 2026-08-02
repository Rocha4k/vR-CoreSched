import { useEffect, useMemo, useRef, useState } from 'react';
import { AdminPanel } from './components/admin/AdminPanel';
import { DashboardView } from './components/dashboard/DashboardView';
import { FloorplanEditor } from './components/floorplan/FloorplanEditor';
import { ProfilesPanel } from './components/profiles/ProfilesPanel';
import { ReportingPanel } from './components/reporting/ReportingPanel';
import { createOperationsConnection } from './lib/realtime';
import {
  ApiError,
  acknowledgeAlert,
  createMaintenanceRecord,
  createUser,
  downloadConsumptionReportCsv,
  downloadConsumptionReportPdf,
  fetchConsumptionReport,
  fetchMe,
  fetchUsers,
  fetchWorkspace,
  login,
  refreshSession,
  saveFloorplan,
  saveFloorplanPin,
  saveMachine,
  saveRule,
  saveUser,
  saveZone,
  toggleLighting,
  updateMe
} from './lib/api';
import type {
  AdminMachine,
  Alert,
  ConsumptionReport,
  CurrentUser,
  DashboardSnapshot,
  FloorplanLayout,
  FloorplanPin,
  FloorplanPoint,
  LoginResponse,
  MachineTelemetry,
  MaintenanceRecord,
  ReportFilters,
  RuleDefinition,
  UserProfile,
  WorkspaceSnapshot,
  Zone
} from './lib/types';

const fallbackWorkspace = createFallbackWorkspace();
const accessTokenStorageKey = 'vrcoresched.access';
const refreshTokenStorageKey = 'vrcoresched.refresh';
const legacyTokenStorageKey = 'vrcoresched.token';
// The simulator publishes one telemetry message per machine per second.
// Batching them into a short window trades N re-renders per second for one.
const telemetryFlushMs = 500;
const workspaceRefreshMs = 60000;

type AppTab = 'dashboard' | 'reports' | 'profiles' | 'admin' | 'floorplan';
type ConnectionState = { tone: 'live' | 'wait' | 'down'; label: string };

const connectionStates = {
  awaitingLogin: { tone: 'wait', label: 'Awaiting sign-in' },
  connecting: { tone: 'wait', label: 'Connecting' },
  live: { tone: 'live', label: 'Live' },
  synced: { tone: 'live', label: 'Synced' },
  reconnecting: { tone: 'wait', label: 'Reconnecting' },
  offline: { tone: 'down', label: 'Offline — local data' },
  lost: { tone: 'down', label: 'Connection lost' },
  noRealtime: { tone: 'wait', label: 'Polling — no realtime' }
} satisfies Record<string, ConnectionState>;

export default function App() {
  const pendingToggles = useRef(new Set<string>());
  const [workspace, setWorkspace] = useState<WorkspaceSnapshot>(fallbackWorkspace);
  const [activeTab, setActiveTab] = useState<AppTab>(readTabFromHash);
  const [connection, setConnection] = useState<ConnectionState>(connectionStates.connecting);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState<string | null>(null);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [authReady, setAuthReady] = useState(false);
  const [loginError, setLoginError] = useState<string | null>(null);
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [report, setReport] = useState<ConsumptionReport | null>(null);
  const [reportLoading, setReportLoading] = useState(false);
  const [reportError, setReportError] = useState<string | null>(null);
  const [reportFilters, setReportFilters] = useState<ReportFilters>(createDefaultReportFilters());

  // Keep the tab in the URL so a refresh or a shared link lands on the same view.
  useEffect(() => {
    window.location.hash = activeTab;
  }, [activeTab]);

  useEffect(() => {
    const onHashChange = () => setActiveTab(readTabFromHash());
    window.addEventListener('hashchange', onHashChange);
    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  useEffect(() => {
    const storedAccess = window.localStorage.getItem(accessTokenStorageKey) ?? window.localStorage.getItem(legacyTokenStorageKey);
    const storedRefresh = window.localStorage.getItem(refreshTokenStorageKey);

    if (storedAccess) {
      setAccessToken(storedAccess);
    }

    if (storedRefresh) {
      setRefreshToken(storedRefresh);
    }

    setAuthReady(true);
  }, []);

  const currentRole = currentUser?.role;

  useEffect(() => {
    if (!accessToken) {
      setCurrentUser(null);
      setUsers([]);
      setReport(null);
      setConnection(connectionStates.awaitingLogin);
      return;
    }

    if (!currentRole) {
      return;
    }

    let mounted = true;
    const hub = createOperationsConnection(accessToken);
    const telemetryBuffer = new Map<string, MachineTelemetry>();
    let flushHandle: number | null = null;

    const refreshWorkspace = async () => {
      try {
        const data = await fetchWorkspace(accessToken, currentRole);
        if (mounted) {
          setWorkspace(data);
          setConnection(current => (current.tone === 'live' ? current : connectionStates.synced));
        }
      } catch {
        if (mounted) {
          setConnection(connectionStates.offline);
        }
      }
    };

    const flushTelemetry = () => {
      flushHandle = null;
      if (!mounted || telemetryBuffer.size === 0) return;

      const batch = new Map(telemetryBuffer);
      telemetryBuffer.clear();
      setWorkspace(current => applyTelemetryBatch(current, batch));
    };

    const handleTelemetry = (telemetry: MachineTelemetry) => {
      if (!mounted) return;

      // Only the newest reading per machine within the window matters.
      telemetryBuffer.set(telemetry.machineId, telemetry);
      if (flushHandle === null) {
        flushHandle = window.setTimeout(flushTelemetry, telemetryFlushMs);
      }
    };

    const handleAlert = (alert: Alert) => {
      if (!mounted) return;

      setWorkspace(current => ({
        ...current,
        dashboard: {
          ...current.dashboard,
          alerts: [alert, ...current.dashboard.alerts.filter(item => item.id !== alert.id)].slice(0, 20)
        }
      }));
    };

    const handleMaintenance = (records: MaintenanceRecord[]) => {
      if (!mounted) return;

      setWorkspace(current => ({
        ...current,
        dashboard: {
          ...current.dashboard,
          maintenanceRecords: records
        }
      }));
    };

    const handleLighting = (lighting: { id: string; zone: string; name: string; isOn: boolean; lastChangedAt: string; lastCommandSource: string }) => {
      if (!mounted) return;
      if (pendingToggles.current.has(lighting.id)) return;

      setWorkspace(current => ({
        ...current,
        dashboard: {
          ...current.dashboard,
          lighting: current.dashboard.lighting.map(item => item.id === lighting.id ? lighting : item)
        }
      }));
    };

    hub.on('telemetry.received', handleTelemetry);
    hub.on('alert.created', handleAlert);
    hub.on('alert.updated', handleAlert);
    hub.on('maintenance.updated', handleMaintenance);
    hub.on('lighting.updated', handleLighting);
    hub.on('rules.updated', refreshWorkspace);
    hub.on('machines.updated', refreshWorkspace);
    hub.on('zones.updated', refreshWorkspace);
    hub.on('floorplan.updated', refreshWorkspace);
    hub.onreconnecting(() => setConnection(connectionStates.reconnecting));
    hub.onreconnected(() => setConnection(connectionStates.live));
    hub.onclose(() => setConnection(connectionStates.lost));

    void hub.start()
      .then(() => setConnection(connectionStates.live))
      .catch(() => setConnection(connectionStates.noRealtime));

    void refreshWorkspace();
    const refreshInterval = window.setInterval(() => { void refreshWorkspace(); }, workspaceRefreshMs);

    return () => {
      mounted = false;
      window.clearInterval(refreshInterval);
      if (flushHandle !== null) {
        window.clearTimeout(flushHandle);
      }
      void hub.stop();
    };
  }, [accessToken, currentRole]);

  useEffect(() => {
    if (!accessToken) {
      return;
    }

    let mounted = true;

    const syncAuth = async () => {
      try {
        const user = await fetchMe(accessToken);
        if (mounted) {
          setCurrentUser(user);
        }
      } catch {
        if (!refreshToken) {
          handleLogout();
          return;
        }

        try {
          const session = await refreshSession(refreshToken);
          persistSession(session);
          if (mounted) {
            setAccessToken(session.accessToken);
            setRefreshToken(session.refreshToken);
            setCurrentUser(session.user);
          }
        } catch {
          handleLogout();
        }
      }
    };

    void syncAuth();

    return () => {
      mounted = false;
    };
  }, [accessToken, refreshToken]);

  useEffect(() => {
    if (!accessToken || !currentUser || activeTab !== 'reports') {
      return;
    }

    let mounted = true;
    setReportLoading(true);
    setReportError(null);

    void fetchConsumptionReport(reportFilters, accessToken)
      .then(data => {
        if (mounted) {
          setReport(data);
        }
      })
      .catch(() => {
        if (mounted) {
          setReportError('Could not load the report.');
        }
      })
      .finally(() => {
        if (mounted) {
          setReportLoading(false);
        }
      });

    return () => {
      mounted = false;
    };
  }, [accessToken, activeTab, currentUser, reportFilters]);

  useEffect(() => {
    if (!accessToken || !currentUser || currentUser.role !== 'Admin') {
      setUsers([]);
      return;
    }

    let mounted = true;
    void fetchUsers(accessToken)
      .then(data => {
        if (mounted) {
          setUsers(data);
        }
      })
      .catch(() => {
        if (mounted) {
          setUsers([]);
        }
      });

    return () => {
      mounted = false;
    };
  }, [accessToken, currentUser?.role]);

  const handleLogin = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const username = String(formData.get('username') ?? '').trim();
    const password = String(formData.get('password') ?? '').trim();

    try {
      const session = await login(username, password);
      persistSession(session);
      setAccessToken(session.accessToken);
      setRefreshToken(session.refreshToken);
      setCurrentUser(session.user);
      setLoginError(null);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setLoginError('Invalid credentials.');
        return;
      }

      if (error instanceof ApiError && error.status === 429) {
        setLoginError('Too many attempts. Wait a minute and try again.');
        return;
      }

      setLoginError('Backend unreachable. Check that the .NET service is running on http://localhost:5080.');
    }
  };

  const handleLogout = () => {
    clearSession();
    setAccessToken(null);
    setRefreshToken(null);
    setCurrentUser(null);
    setUsers([]);
    setReport(null);
    setLoginError(null);
  };

  const refreshWorkspace = async () => {
    if (!accessToken) return;
    const data = await fetchWorkspace(accessToken, currentRole);
    setWorkspace(data);
  };

  const handleToggleLight = async (deviceId: string) => {
    if (!accessToken) return;

    pendingToggles.current.add(deviceId);

    setWorkspace(current => ({
      ...current,
      dashboard: {
        ...current.dashboard,
        lighting: current.dashboard.lighting.map(item => item.id === deviceId ? { ...item, isOn: !item.isOn } : item)
      }
    }));

    try {
      await toggleLighting(deviceId, accessToken);
    } catch {
      await refreshWorkspace();
    } finally {
      pendingToggles.current.delete(deviceId);
    }
  };

  const handleSaveRule = async (rule: RuleDefinition) => {
    if (!accessToken) throw new Error('Not authenticated');
    const saved = await saveRule(rule, accessToken);
    setWorkspace(current => ({ ...current, rules: current.rules.map(item => item.id === saved.id ? saved : item) }));
    return saved;
  };

  const handleSaveMachine = async (machine: AdminMachine) => {
    if (!accessToken) throw new Error('Not authenticated');
    const saved = await saveMachine(machine, accessToken);
    setWorkspace(current => ({ ...current, machines: current.machines.map(item => item.machineId === saved.machineId ? saved : item) }));
    return saved;
  };

  const handleSaveZone = async (zone: Zone) => {
    if (!accessToken) throw new Error('Not authenticated');
    const saved = await saveZone(zone, accessToken);
    setWorkspace(current => ({ ...current, zones: current.zones.map(item => item.zoneId === saved.zoneId ? saved : item) }));
    return saved;
  };

  const handleMovePin = async (pin: FloorplanPin) => {
    if (!accessToken) throw new Error('Not authenticated');
    const savedPin = await saveFloorplanPin(pin, accessToken);

    setWorkspace(current => {
      const floorplan = {
        ...current.floorplan,
        pins: current.floorplan.pins.map(item => item.id === savedPin.id ? savedPin : item)
      };

      const machines = current.machines.map(machine => savedPin.deviceType === 'Machine' && machine.machineId === savedPin.deviceId
        ? { ...machine, locationX: savedPin.x, locationY: savedPin.y }
        : machine);

      return { ...current, floorplan, machines };
    });
  };

  const handleAddBoundaryPoint = async (point: FloorplanPoint) => {
    const latest = parseBoundaryPoints(workspace.floorplan.boundaryPointsJson);
    const updatedFloorplan: FloorplanLayout = {
      ...workspace.floorplan,
      boundaryPointsJson: JSON.stringify([...latest, point])
    };

    setWorkspace(current => ({ ...current, floorplan: updatedFloorplan }));
    if (accessToken) {
      await saveFloorplan(updatedFloorplan, accessToken);
    }
  };

  const handleAcknowledgeAlert = async (alertId: string, note: string) => {
    if (!accessToken) return;

    await acknowledgeAlert(alertId, note, accessToken);
    await refreshWorkspace();
  };

  const handleCreateMaintenance = async (machineId: string, title: string, notes: string, status: string) => {
    if (!accessToken) return;

    await createMaintenanceRecord({ machineId, title, notes, status }, accessToken);
    await refreshWorkspace();
  };

  const handleUpdateProfile = async (request: Parameters<typeof updateMe>[0]) => {
    if (!accessToken || !currentUser) throw new Error('Not authenticated');
    const saved = await updateMe(request, accessToken);
    const nextUser = toCurrentUser(saved);
    setCurrentUser(nextUser);
    setUsers(current => current.map(item => item.username === saved.username ? saved : item));
    return saved;
  };

  const handleSaveUser = async (request: Parameters<typeof saveUser>[0]) => {
    if (!accessToken) throw new Error('Not authenticated');
    const saved = await saveUser(request, accessToken);
    setUsers(current => current.map(item => item.username === saved.username ? saved : item));
    if (currentUser?.username === saved.username) {
      setCurrentUser(toCurrentUser(saved));
    }
    return saved;
  };

  const handleCreateUser = async (request: Parameters<typeof createUser>[0]) => {
    if (!accessToken) throw new Error('Not authenticated');
    const saved = await createUser(request, accessToken);
    setUsers(current => [...current.filter(item => item.username !== saved.username), saved].sort((left, right) => left.fullName.localeCompare(right.fullName)));
    return saved;
  };

  const handleExportCsv = async () => {
    if (!accessToken) return;
    const blob = await downloadConsumptionReportCsv(reportFilters, accessToken);
    downloadBlob(blob, `consumption-report-${reportFilters.month}.csv`);
  };

  const handleExportPdf = async () => {
    if (!accessToken) return;
    const blob = await downloadConsumptionReportPdf(reportFilters, accessToken);
    downloadBlob(blob, `consumption-report-${reportFilters.month}.pdf`);
  };

  const canAccessAdmin = currentUser?.role === 'Admin' || currentUser?.role === 'Supervisor';
  const canAccessRules = currentUser?.role === 'Admin';
  const canCreateMaintenance = currentUser?.role === 'Admin' || currentUser?.role === 'Supervisor';
  const canManageUsers = currentUser?.role === 'Admin';

  const tabs = useMemo(() => {
    const items: Array<{ key: AppTab; label: string }> = [
      { key: 'dashboard', label: 'Operations' },
      { key: 'reports', label: 'Reporting' },
      { key: 'profiles', label: 'Profiles' }
    ];

    if (canAccessAdmin) {
      items.push({ key: 'admin', label: 'Administration' });
      items.push({ key: 'floorplan', label: 'Floor plan' });
    }

    return items;
  }, [canAccessAdmin]);

  if (!authReady) {
    return <div className="app-loading">Restoring session…</div>;
  }

  if (!accessToken || !currentUser) {
    return (
      <div className="auth-wrap">
        <section className="auth-card">
          <div className="auth-card__brand">
            <img src="/logo-256.png" alt="" width={54} height={54} />
            <div>
              <h1>vR-CoreSched</h1>
              <p>Warehouse monitoring &amp; control</p>
            </div>
          </div>
          <form className="auth-form" onSubmit={handleLogin}>
            <label className="field">
              <span className="field__label">Username</span>
              <input name="username" autoComplete="username" autoFocus />
            </label>
            <label className="field">
              <span className="field__label">Password</span>
              <input name="password" type="password" autoComplete="current-password" />
            </label>
            {loginError ? <div className="auth-form__error">{loginError}</div> : null}
            <button type="submit" className="btn btn--primary">Sign in</button>
          </form>
          <div className="auth-card__hint">
            Demo accounts<br />
            <code>operator / operator123</code> · day-to-day control<br />
            <code>supervisor / supervisor123</code> · operational setup<br />
            <code>admin / admin123</code> · rules, users and layout
          </div>
        </section>
      </div>
    );
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div className="brand">
          <img className="brand__mark" src="/logo-mark.png" alt="" width={30} height={30} />
          <span className="brand__text">
            <span className="brand__name">vR-CoreSched</span>
            <span className={`conn conn--${connection.tone}`}>
              <span className="conn__dot" />
              <span className="conn__label">{connection.label}</span>
            </span>
          </span>
        </div>

        <nav className="tabs">
          {tabs.map(tab => (
            <button key={tab.key} type="button" className={activeTab === tab.key ? 'tab is-active' : 'tab'} onClick={() => setActiveTab(tab.key)}>
              {tab.label}
            </button>
          ))}
        </nav>

        <div className="topbar__user">
          <span className="topbar__identity">
            <span>{currentUser.fullName}</span>
            <small>{currentUser.role}</small>
          </span>
          <button type="button" className="btn btn--ghost btn--sm" onClick={handleLogout}>Sign out</button>
        </div>
      </header>

      {activeTab === 'dashboard' ? (
        <DashboardView
          workspace={workspace}
          onToggleLight={handleToggleLight}
          onAcknowledgeAlert={handleAcknowledgeAlert}
          onCreateMaintenance={handleCreateMaintenance}
          canCreateMaintenance={canCreateMaintenance}
        />
      ) : null}

      {activeTab === 'reports' ? (
        <ReportingPanel
          filters={reportFilters}
          report={report}
          machines={workspace.machines}
          zones={workspace.zones}
          loading={reportLoading}
          error={reportError}
          onChangeFilters={setReportFilters}
          onExportCsv={() => void handleExportCsv()}
          onExportPdf={() => void handleExportPdf()}
        />
      ) : null}

      {activeTab === 'profiles' ? (
        <ProfilesPanel
          currentUser={currentUser}
          users={users}
          canManageUsers={canManageUsers}
          onUpdateProfile={handleUpdateProfile}
          onCreateUser={handleCreateUser}
          onSaveUser={handleSaveUser}
        />
      ) : null}

      {activeTab === 'admin' && canAccessAdmin ? (
        <AdminPanel
          rules={workspace.rules}
          machines={workspace.machines}
          zones={workspace.zones}
          canEditRules={canAccessRules}
          onSaveRule={handleSaveRule}
          onSaveMachine={handleSaveMachine}
          onSaveZone={handleSaveZone}
        />
      ) : null}

      {activeTab === 'floorplan' && canAccessAdmin ? (
        <FloorplanEditor
          layout={workspace.floorplan}
          machines={workspace.machines}
          lighting={workspace.dashboard.lighting}
          onMovePin={pin => void handleMovePin(pin)}
          onAddBoundaryPoint={point => void handleAddBoundaryPoint(point)}
        />
      ) : null}
    </main>
  );
}

function createFallbackWorkspace(): WorkspaceSnapshot {
  const generatedAt = new Date().toISOString();

  const dashboard: DashboardSnapshot = {
    generatedAt,
    machines: [
      { machineId: 'press-01', name: 'Hydraulic Press', zone: 'production-area', isOnline: true, lastSeen: generatedAt, temperatureC: 78.4, vibrationMs2: 3.2, rpm: 1210, energyKwh: 9.5, severity: 'Info' },
      { machineId: 'line-01', name: 'Assembly Line', zone: 'assembly-line', isOnline: true, lastSeen: generatedAt, temperatureC: 66.1, vibrationMs2: 1.7, rpm: 812, energyKwh: 6.2, severity: 'Info' },
      { machineId: 'belt-01', name: 'Conveyor Belt', zone: 'aisle-a', isOnline: true, lastSeen: generatedAt, temperatureC: 59.3, vibrationMs2: 1.1, rpm: 404, energyKwh: 3.4, severity: 'Info' }
    ],
    lighting: [
      { id: 'light-loading', zone: 'loading-bay', name: 'Loading Bay Light', isOn: true, lastChangedAt: generatedAt, lastCommandSource: 'seed' },
      { id: 'light-aisle-a', zone: 'aisle-a', name: 'Aisle A Light', isOn: true, lastChangedAt: generatedAt, lastCommandSource: 'seed' },
      { id: 'light-aisle-b', zone: 'aisle-b', name: 'Aisle B Light', isOn: false, lastChangedAt: generatedAt, lastCommandSource: 'seed' },
      { id: 'light-office', zone: 'offices', name: 'Office Light', isOn: true, lastChangedAt: generatedAt, lastCommandSource: 'seed' }
    ],
    alerts: [],
    aggregates: [{ id: 'agg-1', scopeType: 'Machine', scopeId: 'press-01', periodStart: generatedAt, periodEnd: generatedAt, averageKwh: 8.9, totalKwh: 78.2, costEuro: 14.08 }],
    maintenanceRecords: [
      { id: 'maint-1', machineId: 'press-01', alertId: null, title: 'Preventive press inspection', status: 'Closed', notes: 'Lubrication completed.', createdBy: 'system', createdAt: generatedAt, closedAt: generatedAt, closedBy: 'supervisor' }
    ]
  };

  return {
    dashboard,
    rules: [
      { id: 'rule-temp-vib-press', code: 'TEMP_VIB_001', name: 'Press critical on temperature and vibration', targetType: 'Machine', targetId: 'press-01', severity: 'Critical', temperatureThreshold: 85, vibrationThreshold: 8, durationSeconds: 5, cooldownSeconds: 30, isEnabled: true },
      { id: 'rule-temp-vib-line', code: 'TEMP_VIB_002', name: 'Assembly line under stress', targetType: 'Machine', targetId: 'line-01', severity: 'Warning', temperatureThreshold: 82, vibrationThreshold: 7, durationSeconds: 6, cooldownSeconds: 30, isEnabled: true },
      { id: 'rule-light-off-hours', code: 'LIGHT_WASTE_001', name: 'Lighting outside operating hours', targetType: 'Zone', targetId: 'aisle-a', severity: 'Info', temperatureThreshold: 0, vibrationThreshold: 0, durationSeconds: 0, cooldownSeconds: 60, isEnabled: false }
    ],
    zones: [
      { zoneId: 'loading-bay', name: 'Loading Bay', description: 'Goods receiving and dispatch.', color: '#d8d8de', isActive: true },
      { zoneId: 'production-area', name: 'Production Area', description: 'Main heavy machinery floor.', color: '#b4b4bd', isActive: true },
      { zoneId: 'assembly-line', name: 'Assembly Line', description: 'Assembly and finishing.', color: '#9a9aa4', isActive: true },
      { zoneId: 'aisle-a', name: 'Aisle A', description: 'Main aisle.', color: '#80808b', isActive: true },
      { zoneId: 'aisle-b', name: 'Aisle B', description: 'Secondary aisle.', color: '#6a6a75', isActive: true },
      { zoneId: 'offices', name: 'Offices', description: 'Administrative area.', color: '#55555f', isActive: true }
    ],
    machines: [
      { machineId: 'press-01', name: 'Hydraulic Press', zoneId: 'production-area', isEnabled: true, isOnline: true, lastSeen: generatedAt, temperatureC: 78.4, vibrationMs2: 3.2, rpm: 1210, energyKwh: 9.5, severity: 'Info', locationX: 22, locationY: 28 },
      { machineId: 'line-01', name: 'Assembly Line', zoneId: 'assembly-line', isEnabled: true, isOnline: true, lastSeen: generatedAt, temperatureC: 66.1, vibrationMs2: 1.7, rpm: 812, energyKwh: 6.2, severity: 'Info', locationX: 50, locationY: 34 },
      { machineId: 'belt-01', name: 'Conveyor Belt', zoneId: 'aisle-a', isEnabled: true, isOnline: true, lastSeen: generatedAt, temperatureC: 59.3, vibrationMs2: 1.1, rpm: 404, energyKwh: 3.4, severity: 'Info', locationX: 65, locationY: 45 }
    ],
    floorplan: {
      id: 1,
      name: 'Main Warehouse',
      canvasWidth: 1200,
      canvasHeight: 760,
      textureKey: 'warehouse-grid',
      boundaryPointsJson: JSON.stringify([
        { x: 8, y: 14 },
        { x: 92, y: 14 },
        { x: 96, y: 26 },
        { x: 96, y: 86 },
        { x: 8, y: 86 },
        { x: 8, y: 24 }
      ]),
      updatedAt: generatedAt,
      pins: [
        { id: 1, deviceType: 'Light', deviceId: 'light-loading', label: 'Loading Bay Light', x: 14, y: 16, isVisible: true, zoneId: 'loading-bay' },
        { id: 2, deviceType: 'Light', deviceId: 'light-aisle-a', label: 'Aisle A Light', x: 42, y: 42, isVisible: true, zoneId: 'aisle-a' },
        { id: 3, deviceType: 'Light', deviceId: 'light-aisle-b', label: 'Aisle B Light', x: 72, y: 42, isVisible: true, zoneId: 'aisle-b' },
        { id: 4, deviceType: 'Light', deviceId: 'light-office', label: 'Office Light', x: 83, y: 16, isVisible: true, zoneId: 'offices' },
        { id: 5, deviceType: 'Machine', deviceId: 'press-01', label: 'Hydraulic Press', x: 22, y: 28, isVisible: true, zoneId: 'production-area' },
        { id: 6, deviceType: 'Machine', deviceId: 'line-01', label: 'Assembly Line', x: 50, y: 34, isVisible: true, zoneId: 'assembly-line' },
        { id: 7, deviceType: 'Machine', deviceId: 'belt-01', label: 'Conveyor Belt', x: 65, y: 45, isVisible: true, zoneId: 'aisle-a' }
      ]
    }
  };
}

function readTabFromHash(): AppTab {
  const candidate = window.location.hash.replace('#', '');
  const known: AppTab[] = ['dashboard', 'reports', 'profiles', 'admin', 'floorplan'];
  return known.includes(candidate as AppTab) ? candidate as AppTab : 'dashboard';
}

function applyTelemetryBatch(current: WorkspaceSnapshot, batch: Map<string, MachineTelemetry>): WorkspaceSnapshot {
  let changed = false;

  const dashboardMachines = current.dashboard.machines.map(machine => {
    const telemetry = batch.get(machine.machineId);
    if (!telemetry) return machine;

    changed = true;
    return {
      ...machine,
      name: telemetry.name,
      zone: telemetry.zone,
      lastSeen: telemetry.timestamp,
      temperatureC: telemetry.temperatureC,
      vibrationMs2: telemetry.vibrationMs2,
      rpm: telemetry.rpm,
      energyKwh: telemetry.energyKwh,
      isOnline: true
    };
  });

  const machines = current.machines.map(machine => {
    const telemetry = batch.get(machine.machineId);
    if (!telemetry) return machine;

    return {
      ...machine,
      name: telemetry.name,
      zoneId: telemetry.zone,
      lastSeen: telemetry.timestamp,
      temperatureC: telemetry.temperatureC,
      vibrationMs2: telemetry.vibrationMs2,
      rpm: telemetry.rpm,
      energyKwh: telemetry.energyKwh,
      isOnline: true
    };
  });

  // Telemetry for an unknown machine must not force a re-render.
  if (!changed) return current;

  return { ...current, dashboard: { ...current.dashboard, machines: dashboardMachines }, machines };
}

function parseBoundaryPoints(json: string): FloorplanPoint[] {
  try {
    const parsed = JSON.parse(json) as FloorplanPoint[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function persistSession(session: LoginResponse) {
  window.localStorage.setItem(accessTokenStorageKey, session.accessToken);
  window.localStorage.setItem(refreshTokenStorageKey, session.refreshToken);
  window.localStorage.setItem(legacyTokenStorageKey, session.accessToken);
}

function clearSession() {
  window.localStorage.removeItem(accessTokenStorageKey);
  window.localStorage.removeItem(refreshTokenStorageKey);
  window.localStorage.removeItem(legacyTokenStorageKey);
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  window.URL.revokeObjectURL(url);
}

function createDefaultReportFilters(): ReportFilters {
  return {
    month: new Date().toISOString().slice(0, 7),
    machineId: '',
    zoneId: ''
  };
}

function toCurrentUser(profile: UserProfile): CurrentUser {
  return {
    username: profile.username,
    fullName: profile.fullName,
    role: profile.role,
    isActive: profile.isActive,
    lastLoginAt: profile.lastLoginAt
  };
}
