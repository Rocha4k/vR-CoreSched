export type MachineState = {
  machineId: string;
  name: string;
  zone: string;
  isOnline: boolean;
  lastSeen: string;
  temperatureC: number;
  vibrationMs2: number;
  rpm: number;
  energyKwh: number;
  severity: string;
};

export type MachineTelemetry = {
  machineId: string;
  name: string;
  zone: string;
  timestamp: string;
  temperatureC: number;
  vibrationMs2: number;
  rpm: number;
  energyKwh: number;
  source: string;
};

export type LightingDevice = {
  id: string;
  zone: string;
  name: string;
  isOn: boolean;
  lastChangedAt: string;
  lastCommandSource: string;
};

export type RuleDefinition = {
  id: string;
  code: string;
  name: string;
  targetType: string;
  targetId: string | null;
  severity: string;
  temperatureThreshold: number;
  vibrationThreshold: number;
  durationSeconds: number;
  cooldownSeconds: number;
  isEnabled: boolean;
};

export type AdminMachine = {
  machineId: string;
  name: string;
  zoneId: string;
  isEnabled: boolean;
  isOnline: boolean;
  lastSeen: string | null;
  temperatureC: number;
  vibrationMs2: number;
  rpm: number;
  energyKwh: number;
  severity: string;
  locationX: number;
  locationY: number;
};

export type Zone = {
  zoneId: string;
  name: string;
  description: string;
  color: string;
  isActive: boolean;
};

export type FloorplanPoint = {
  x: number;
  y: number;
};

export type FloorplanPin = {
  id: number;
  deviceType: string;
  deviceId: string;
  label: string;
  x: number;
  y: number;
  isVisible: boolean;
  zoneId: string;
};

export type FloorplanLayout = {
  id: number;
  name: string;
  canvasWidth: number;
  canvasHeight: number;
  textureKey: string;
  boundaryPointsJson: string;
  updatedAt: string;
  pins: FloorplanPin[];
};

export type Alert = {
  id: string;
  machineId: string;
  severity: string;
  ruleCode: string;
  message: string;
  startTime: string;
  endTime: string | null;
  isAcknowledged: boolean;
};

export type CurrentUser = {
  username: string;
  fullName: string;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  user: CurrentUser;
};

export type UserProfile = {
  username: string;
  fullName: string;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type UpdateProfileRequest = {
  fullName: string;
  currentPassword?: string | null;
  newPassword?: string | null;
};

export type UpsertUserRequest = {
  username: string;
  fullName: string;
  role: string;
  isActive: boolean;
  password?: string | null;
};

export type MaintenanceRecord = {
  id: string;
  machineId: string;
  alertId: string | null;
  title: string;
  status: string;
  notes: string;
  createdBy: string;
  createdAt: string;
  closedAt: string | null;
  closedBy: string | null;
};

export type ConsumptionAggregate = {
  id: string;
  scopeType: string;
  scopeId: string;
  periodStart: string;
  periodEnd: string;
  averageKwh: number;
  totalKwh: number;
  costEuro: number;
};

export type DashboardSnapshot = {
  generatedAt: string;
  machines: MachineState[];
  lighting: LightingDevice[];
  alerts: Alert[];
  aggregates: ConsumptionAggregate[];
  maintenanceRecords: MaintenanceRecord[];
};

export type WorkspaceSnapshot = {
  dashboard: DashboardSnapshot;
  rules: RuleDefinition[];
  zones: Zone[];
  machines: AdminMachine[];
  floorplan: FloorplanLayout;
};

export type ConsumptionReportRow = {
  scopeType: string;
  scopeId: string;
  label: string;
  machineId: string | null;
  machineName: string | null;
  zoneId: string | null;
  zoneName: string | null;
  periodStart: string;
  periodEnd: string;
  averageKwh: number;
  totalKwh: number;
  costEuro: number;
};

export type ConsumptionReport = {
  month: string;
  machineId: string | null;
  zoneId: string | null;
  generatedAt: string;
  totalKwh: number;
  totalCostEuro: number;
  rows: ConsumptionReportRow[];
};

export type ReportFilters = {
  month: string;
  machineId: string;
  zoneId: string;
};


