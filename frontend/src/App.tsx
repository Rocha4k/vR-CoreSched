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

type AppTab = 'dashboard' | 'reports' | 'profiles' | 'admin' | 'floorplan';

export default function App() {
  const pendingToggles = useRef(new Set<string>());
  const [workspace, setWorkspace] = useState<WorkspaceSnapshot>(fallbackWorkspace);
  const [activeTab, setActiveTab] = useState<AppTab>('dashboard');
  const [connectionStatus, setConnectionStatus] = useState('a ligar...');
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

  useEffect(() => {
    if (!accessToken) {
      setCurrentUser(null);
      setUsers([]);
      setReport(null);
      setConnectionStatus('a aguardar login...');
      return;
    }

    let mounted = true;
    const connection = createOperationsConnection(accessToken);

    const refreshWorkspace = async () => {
      try {
        const data = await fetchWorkspace(accessToken);
        if (mounted) {
          setWorkspace(data);
          setConnectionStatus('sincronizado');
        }
      } catch {
        if (mounted) {
          setConnectionStatus('modo offline com dados locais');
        }
      }
    };

    const handleTelemetry = (telemetry: MachineTelemetry) => {
      if (!mounted) return;

      setWorkspace(current => ({
        ...current,
        dashboard: {
          ...current.dashboard,
          machines: current.dashboard.machines.map(machine => machine.machineId === telemetry.machineId
            ? {
                ...machine,
                name: telemetry.name,
                zone: telemetry.zone,
                lastSeen: telemetry.timestamp,
                temperatureC: telemetry.temperatureC,
                vibrationMs2: telemetry.vibrationMs2,
                rpm: telemetry.rpm,
                energyKwh: telemetry.energyKwh,
                isOnline: true
              }
            : machine)
        },
        machines: current.machines.map(machine => machine.machineId === telemetry.machineId
          ? {
              ...machine,
              name: telemetry.name,
              zoneId: telemetry.zone,
              lastSeen: telemetry.timestamp,
              temperatureC: telemetry.temperatureC,
              vibrationMs2: telemetry.vibrationMs2,
              rpm: telemetry.rpm,
              energyKwh: telemetry.energyKwh,
              isOnline: true
            }
          : machine)
      }));
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

    connection.on('telemetry.received', handleTelemetry);
    connection.on('alert.created', handleAlert);
    connection.on('alert.updated', handleAlert);
    connection.on('maintenance.updated', handleMaintenance);
    connection.on('lighting.updated', handleLighting);
    connection.on('rules.updated', refreshWorkspace);
    connection.on('machines.updated', refreshWorkspace);
    connection.on('zones.updated', refreshWorkspace);
    connection.on('floorplan.updated', refreshWorkspace);
    connection.onreconnecting(() => setConnectionStatus('a reconectar...'));
    connection.onreconnected(() => setConnectionStatus('reconectado'));
    connection.onclose(() => setConnectionStatus('ligação perdida'));

    void connection.start()
      .then(() => setConnectionStatus('ligado em tempo real'))
      .catch(() => setConnectionStatus('sem SignalR, a usar fallback local'));

    void refreshWorkspace();
    const refreshInterval = window.setInterval(() => { void refreshWorkspace(); }, 60000);

    return () => {
      mounted = false;
      window.clearInterval(refreshInterval);
      void connection.stop();
    };
  }, [accessToken]);

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
          setReportError('Não foi possível carregar o relatório.');
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
        setLoginError('Credenciais inválidas.');
        return;
      }

      setLoginError('Backend indisponível. Verifica se o serviço .NET está a correr em http://localhost:5080.');
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
    const data = await fetchWorkspace(accessToken);
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
      { key: 'dashboard', label: 'Operação' },
      { key: 'reports', label: 'Reporting' },
      { key: 'profiles', label: 'Perfis' }
    ];

    if (canAccessAdmin) {
      items.push({ key: 'admin', label: 'Administração' });
      items.push({ key: 'floorplan', label: 'Planta' });
    }

    return items;
  }, [canAccessAdmin]);

  return (
    <main className="app-shell">
      {!authReady ? <div className="panel auth-panel"><p>A carregar sessão...</p></div> : null}

      {accessToken && currentUser ? (
        <>
          <header className="topbar">
            <div>
              <span className="topbar__brand">vR-CoreSched</span>
              <span className="topbar__status">{connectionStatus}</span>
            </div>
            <div className="topbar__user">
              <span>{currentUser.fullName}</span>
              <small>{currentUser.role}</small>
              <button type="button" className="tab" onClick={handleLogout}>Sair</button>
            </div>
            <nav className="tabs">
              {tabs.map(tab => (
                <button key={tab.key} type="button" className={activeTab === tab.key ? 'tab is-active' : 'tab'} onClick={() => setActiveTab(tab.key)}>
                  {tab.label}
                </button>
              ))}
            </nav>
          </header>

          {activeTab === 'dashboard' ? (
            <DashboardView
              workspace={workspace}
              connectionStatus={connectionStatus}
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

          {reportError ? <div className="panel report-error">{reportError}</div> : null}
        </>
      ) : (
        <section className="panel auth-panel auth-panel--login">
          <div>
            <p className="eyebrow">Acesso seguro</p>
            <h1>Entre com o seu perfil</h1>
            <p className="lead">Operador para controlo do dia a dia, Supervisor para configuração operacional e Admin para regras, utilizadores e estrutura global.</p>
          </div>
          <form className="auth-form" onSubmit={handleLogin}>
            <input name="username" placeholder="Username" autoComplete="username" />
            <input name="password" type="password" placeholder="Password" autoComplete="current-password" />
            {loginError ? <div className="auth-form__error">{loginError}</div> : null}
            <button type="submit">Entrar</button>
            <small>Credenciais demo: operator/operator123, supervisor/supervisor123, admin/admin123</small>
          </form>
        </section>
      )}
    </main>
  );
}

function createFallbackWorkspace(): WorkspaceSnapshot {
  const generatedAt = new Date().toISOString();

  const dashboard: DashboardSnapshot = {
    generatedAt,
    machines: [
      { machineId: 'press-01', name: 'Prensa Hidráulica', zone: 'zona-producao', isOnline: true, lastSeen: generatedAt, temperatureC: 78.4, vibrationMs2: 3.2, rpm: 1210, energyKwh: 9.5, severity: 'Info' },
      { machineId: 'line-01', name: 'Linha de Montagem', zone: 'linha-montagem', isOnline: true, lastSeen: generatedAt, temperatureC: 66.1, vibrationMs2: 1.7, rpm: 812, energyKwh: 6.2, severity: 'Info' },
      { machineId: 'belt-01', name: 'Tapete Rolante', zone: 'corredor-a', isOnline: true, lastSeen: generatedAt, temperatureC: 59.3, vibrationMs2: 1.1, rpm: 404, energyKwh: 3.4, severity: 'Info' }
    ],
    lighting: [
      { id: 'light-carga', zone: 'zona-carga', name: 'Luz da Zona de Carga', isOn: true, lastChangedAt: generatedAt, lastCommandSource: 'seed' },
      { id: 'light-corridor-a', zone: 'corredor-a', name: 'Luz do Corredor A', isOn: true, lastChangedAt: generatedAt, lastCommandSource: 'seed' },
      { id: 'light-corridor-b', zone: 'corredor-b', name: 'Luz do Corredor B', isOn: false, lastChangedAt: generatedAt, lastCommandSource: 'seed' },
      { id: 'light-office', zone: 'escritorios', name: 'Luz dos Escritórios', isOn: true, lastChangedAt: generatedAt, lastCommandSource: 'seed' }
    ],
    alerts: [],
    aggregates: [{ id: 'agg-1', scopeType: 'Machine', scopeId: 'press-01', periodStart: generatedAt, periodEnd: generatedAt, averageKwh: 8.9, totalKwh: 78.2, costEuro: 14.08 }],
    maintenanceRecords: [
      { id: 'maint-1', machineId: 'press-01', alertId: null, title: 'Verificação preventiva da prensa', status: 'Closed', notes: 'Lubrificação concluída.', createdBy: 'system', createdAt: generatedAt, closedAt: generatedAt, closedBy: 'supervisor' }
    ]
  };

  return {
    dashboard,
    rules: [
      { id: 'rule-temp-vib-press', code: 'TEMP_VIB_001', name: 'Prensa crítica por temperatura e vibração', targetType: 'Machine', targetId: 'press-01', severity: 'Critical', temperatureThreshold: 85, vibrationThreshold: 8, durationSeconds: 5, cooldownSeconds: 30, isEnabled: true },
      { id: 'rule-temp-vib-line', code: 'TEMP_VIB_002', name: 'Linha de montagem sob stress', targetType: 'Machine', targetId: 'line-01', severity: 'Warning', temperatureThreshold: 82, vibrationThreshold: 7, durationSeconds: 6, cooldownSeconds: 30, isEnabled: true },
      { id: 'rule-light-off-hours', code: 'LIGHT_WASTE_001', name: 'Luz fora de horário', targetType: 'Zone', targetId: 'corredor-a', severity: 'Info', temperatureThreshold: 0, vibrationThreshold: 0, durationSeconds: 0, cooldownSeconds: 60, isEnabled: false }
    ],
    zones: [
      { zoneId: 'zona-carga', name: 'Zona de Carga', description: 'Área de receção e expedição.', color: '#f59e0b', isActive: true },
      { zoneId: 'zona-producao', name: 'Zona de Produção', description: 'Área principal das máquinas pesadas.', color: '#22c55e', isActive: true },
      { zoneId: 'linha-montagem', name: 'Linha de Montagem', description: 'Montagem e acabamento.', color: '#38bdf8', isActive: true },
      { zoneId: 'corredor-a', name: 'Corredor A', description: 'Corredor principal.', color: '#a78bfa', isActive: true },
      { zoneId: 'corredor-b', name: 'Corredor B', description: 'Corredor secundário.', color: '#f97316', isActive: true },
      { zoneId: 'escritorios', name: 'Escritórios', description: 'Zona administrativa.', color: '#f43f5e', isActive: true }
    ],
    machines: [
      { machineId: 'press-01', name: 'Prensa Hidráulica', zoneId: 'zona-producao', isEnabled: true, isOnline: true, lastSeen: generatedAt, temperatureC: 78.4, vibrationMs2: 3.2, rpm: 1210, energyKwh: 9.5, severity: 'Info', locationX: 22, locationY: 28 },
      { machineId: 'line-01', name: 'Linha de Montagem', zoneId: 'linha-montagem', isEnabled: true, isOnline: true, lastSeen: generatedAt, temperatureC: 66.1, vibrationMs2: 1.7, rpm: 812, energyKwh: 6.2, severity: 'Info', locationX: 50, locationY: 34 },
      { machineId: 'belt-01', name: 'Tapete Rolante', zoneId: 'corredor-a', isEnabled: true, isOnline: true, lastSeen: generatedAt, temperatureC: 59.3, vibrationMs2: 1.1, rpm: 404, energyKwh: 3.4, severity: 'Info', locationX: 65, locationY: 45 }
    ],
    floorplan: {
      id: 1,
      name: 'Armazém Principal',
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
        { id: 1, deviceType: 'Light', deviceId: 'light-carga', label: 'Luz da Zona de Carga', x: 14, y: 16, isVisible: true, zoneId: 'zona-carga' },
        { id: 2, deviceType: 'Light', deviceId: 'light-corridor-a', label: 'Luz do Corredor A', x: 42, y: 42, isVisible: true, zoneId: 'corredor-a' },
        { id: 3, deviceType: 'Light', deviceId: 'light-corridor-b', label: 'Luz do Corredor B', x: 72, y: 42, isVisible: true, zoneId: 'corredor-b' },
        { id: 4, deviceType: 'Light', deviceId: 'light-office', label: 'Luz dos Escritórios', x: 83, y: 16, isVisible: true, zoneId: 'escritorios' },
        { id: 5, deviceType: 'Machine', deviceId: 'press-01', label: 'Prensa Hidráulica', x: 22, y: 28, isVisible: true, zoneId: 'zona-producao' },
        { id: 6, deviceType: 'Machine', deviceId: 'line-01', label: 'Linha de Montagem', x: 50, y: 34, isVisible: true, zoneId: 'linha-montagem' },
        { id: 7, deviceType: 'Machine', deviceId: 'belt-01', label: 'Tapete Rolante', x: 65, y: 45, isVisible: true, zoneId: 'corredor-a' }
      ]
    }
  };
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
