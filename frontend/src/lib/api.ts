import type {
  AdminMachine,
  ConsumptionReport,
  CurrentUser,
  FloorplanLayout,
  FloorplanPin,
  LoginResponse,
  MaintenanceRecord,
  ReportFilters,
  RuleDefinition,
  UpdateProfileRequest,
  UpsertUserRequest,
  UserProfile,
  WorkspaceSnapshot,
  Zone
} from './types';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5080';

export class ApiError extends Error {
  readonly status?: number;

  constructor(message: string, status?: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

async function requestJson<T>(path: string, init?: RequestInit, token?: string | null): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${API_URL}${path}`, {
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(init?.headers ?? {})
      },
      ...init
    });
  } catch {
    throw new ApiError('backend-unreachable');
  }

  if (!response.ok) {
    throw new ApiError(`Request failed: ${response.status}`, response.status);
  }

  return response.json() as Promise<T>;
}

async function requestBlob(path: string, token: string): Promise<Blob> {
  const response = await fetch(`${API_URL}${path}`, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  return response.blob();
}

function buildReportQuery(filters: ReportFilters): string {
  const params = new URLSearchParams({ month: filters.month });
  if (filters.machineId) {
    params.set('machineId', filters.machineId);
  }
  if (filters.zoneId) {
    params.set('zoneId', filters.zoneId);
  }

  return params.toString();
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  return requestJson<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password })
  });
}

export async function refreshSession(refreshToken: string): Promise<LoginResponse> {
  return requestJson<LoginResponse>('/api/auth/refresh', {
    method: 'POST',
    body: JSON.stringify({ refreshToken })
  });
}

export async function fetchMe(token: string): Promise<CurrentUser> {
  return requestJson<CurrentUser>('/api/auth/me', undefined, token);
}

export async function fetchWorkspace(token: string): Promise<WorkspaceSnapshot> {
  const [dashboard, rules, zones, machines, floorplan] = await Promise.all([
    requestJson<WorkspaceSnapshot['dashboard']>('/api/dashboard', undefined, token),
    requestJson<RuleDefinition[]>('/api/rules', undefined, token),
    requestJson<Zone[]>('/api/zones', undefined, token),
    requestJson<AdminMachine[]>('/api/admin/machines', undefined, token),
    requestJson<FloorplanLayout>('/api/floorplan', undefined, token)
  ]);

  return { dashboard, rules, zones, machines, floorplan };
}

export async function fetchUsers(token: string): Promise<UserProfile[]> {
  return requestJson<UserProfile[]>('/api/users', undefined, token);
}

export async function updateMe(request: UpdateProfileRequest, token: string): Promise<UserProfile> {
  return requestJson<UserProfile>('/api/users/me', {
    method: 'PUT',
    body: JSON.stringify(request)
  }, token);
}

export async function saveUser(request: UpsertUserRequest, token: string): Promise<UserProfile> {
  return requestJson<UserProfile>(`/api/users/${encodeURIComponent(request.username)}`, {
    method: 'PUT',
    body: JSON.stringify(request)
  }, token);
}

export async function createUser(request: UpsertUserRequest, token: string): Promise<UserProfile> {
  return requestJson<UserProfile>('/api/users', {
    method: 'POST',
    body: JSON.stringify(request)
  }, token);
}

export async function fetchConsumptionReport(filters: ReportFilters, token: string): Promise<ConsumptionReport> {
  const query = buildReportQuery(filters);
  return requestJson<ConsumptionReport>(`/api/reports/consumption?${query}`, undefined, token);
}

export async function downloadConsumptionReportCsv(filters: ReportFilters, token: string): Promise<Blob> {
  return requestBlob(`/api/reports/consumption.csv?${buildReportQuery(filters)}`, token);
}

export async function downloadConsumptionReportPdf(filters: ReportFilters, token: string): Promise<Blob> {
  return requestBlob(`/api/reports/consumption.pdf?${buildReportQuery(filters)}`, token);
}

export async function toggleLighting(deviceId: string, token: string): Promise<void> {
  await requestJson(`/api/lighting/${deviceId}/toggle`, { method: 'POST' }, token);
}

export async function saveRule(rule: RuleDefinition, token: string): Promise<RuleDefinition> {
  return requestJson<RuleDefinition>(`/api/rules/${rule.id}`, {
    method: 'PUT',
    body: JSON.stringify(rule)
  }, token);
}

export async function saveMachine(machine: AdminMachine, token: string): Promise<AdminMachine> {
  return requestJson<AdminMachine>(`/api/admin/machines/${machine.machineId}`, {
    method: 'PUT',
    body: JSON.stringify(machine)
  }, token);
}

export async function saveZone(zone: Zone, token: string): Promise<Zone> {
  return requestJson<Zone>(`/api/zones/${zone.zoneId}`, {
    method: 'PUT',
    body: JSON.stringify(zone)
  }, token);
}

export async function saveFloorplan(layout: FloorplanLayout, token: string): Promise<FloorplanLayout> {
  return requestJson<FloorplanLayout>('/api/floorplan', {
    method: 'PUT',
    body: JSON.stringify(layout)
  }, token);
}

export async function saveFloorplanPin(pin: FloorplanPin, token: string): Promise<FloorplanPin> {
  return requestJson<FloorplanPin>(`/api/floorplan/pins/${pin.id}`, {
    method: 'PUT',
    body: JSON.stringify(pin)
  }, token);
}

export async function acknowledgeAlert(alertId: string, note: string, token: string): Promise<void> {
  await requestJson(`/api/alerts/${alertId}/acknowledge`, {
    method: 'POST',
    body: JSON.stringify({ note })
  }, token);
}

export async function createMaintenanceRecord(record: { machineId: string; title: string; notes: string; status: string }, token: string): Promise<MaintenanceRecord> {
  return requestJson<MaintenanceRecord>('/api/maintenance', {
    method: 'POST',
    body: JSON.stringify(record)
  }, token);
}

export { API_URL };