<div align="center">

<img src="img/vR-Clean.png" alt="vR-CoreSched" width="220">

# vR-CoreSched

Warehouse monitoring and control platform featuring simulated IoT telemetry, MQTT, real-time rules engine, interactive lighting, and consumption analytics.

![.NET Core](https://img.shields.io/badge/.NET%20Core-111111?style=flat-square&logo=.net&logoColor=white)
![React](https://img.shields.io/badge/React-111111?style=flat-square&logo=react&logoColor=white)
![MQTT](https://img.shields.io/badge/MQTT-111111?style=flat-square&logo=eclipse-mosquitto&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-111111?style=flat-square&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-111111?style=flat-square&logo=docker&logoColor=white)

</div>

---

## Overview

vR-CoreSched is an integrated solution designed for industrial warehouse management, monitoring, and automation. By ingesting telemetry via MQTT and processing data in real time through a dynamic rules engine, the platform enables live tracking of machine status, critical alert dispatching, and reactive lighting system controls directly on an interactive map layout.

The main focus is operational efficiency and rapid response time, bridging the gap between raw high-frequency sensor telemetry and aggregated historical data for cost and consumption analytics.

---

## System Modules

* **Real-Time Operations** — Continuous telemetry ingestion, instantaneous alert triggering, and reactive lighting control.
* **Rules & Equipment Administration** — Dynamic management of business rules, machines, and zones backed by robust persistence.
* **Interactive SVG Layout** — Dynamic warehouse floor plan based on SVG, featuring editable boundaries and repositionable hotspots directly within the UI.
* **Analytics & Aggregation** — Automated hourly consumption aggregation for optimized, ultra-fast reporting.
* **Alert & Maintenance Workflow** — Event acknowledgment system with automatic logging for maintenance history and auditing.
* **Security & Profiles** — JWT-based authentication featuring rotating refresh tokens and three distinct access levels: Operator, Supervisor, and Admin.
* **Advanced Reporting** — Granular reporting with filters by month, machine, and zone, supporting CSV and PDF exports.

---

## MVP Objectives

1. **Industrial Simulation:** Simulate real-world machinery by generating temperature, vibration, RPM, and power consumption metrics.
2. **Real-Time Ingestion:** Ingest telemetry via an MQTT broker and instantly evaluate/trigger critical system alerts.
3. **Visual Control:** Deliver a web UI to toggle and monitor lighting over an SVG floor plan with instant updates (via SignalR).
4. **Data Efficiency:** Aggregate energy consumption hourly to output streamlined financial and cost reports.

---

## How to Run

Requires the **.NET 8 SDK and runtime**, **Node.js 18+**, and **Docker**.

### 1. Spin up the Infrastructure (Docker)
Ensure Docker is installed and running, then execute the following command in the project root (where `docker-compose.yml` is located):

```bash
docker compose up -d
```

This starts PostgreSQL (`5432`), Redis (`6379`), and Mosquitto (`1883`). If port `5432` is already taken by a local PostgreSQL install, either stop that service or point the backend at another port with `ConnectionStrings__WarehouseDb`.

### 2. Backend API

```bash
cd backend/src/Warehouse.Backend
dotnet run
```

Listens on `http://localhost:5080`, applies EF Core migrations, and seeds demo data on startup. Swagger UI is available at `/swagger` in Development.

### 3. Simulator

```bash
cd simulator
dotnet run
```

Publishes machine telemetry every second over MQTT and drifts the lighting state occasionally.

### 4. Frontend

```bash
cd frontend
npm install
npm run dev
```

Opens on `http://localhost:5173`. Demo credentials: `operator/operator123`, `supervisor/supervisor123`, `admin/admin123`.

---

## Health & Observability

| Endpoint | Purpose |
| --- | --- |
| `GET /health` | Liveness — the process is up. |
| `GET /health/ready` | Readiness — checks PostgreSQL and the MQTT broker. Returns `Degraded` when the broker is unreachable, since historical data still serves. |

Structured logging goes through Serilog (console sink, levels configurable under the `Serilog` section).

---

## Configuration

All keys live under `Warehouse` in `appsettings.json` and can be overridden with environment variables (`Warehouse__MqttHost=...`).

| Key | Default | Description |
| --- | --- | --- |
| `MqttHost` / `MqttPort` | `localhost` / `1883` | MQTT broker endpoint. |
| `AggregationIntervalMinutes` | `60` | Consumption aggregation window. |
| `AlertCooldownSeconds` | `30` | Minimum spacing between repeated alerts. |
| `OfflineThresholdSeconds` | `10` | Silence after which a machine counts as offline. |
| `OfflineScanSeconds` | `5` | Offline detection sweep interval. |
| `RuleCacheSeconds` | `30` | TTL of the rule cache used on the telemetry hot path. |
| `TelemetryRetentionDays` | `7` | Raw telemetry retention; `0` disables pruning. |
| `SnapshotAlertLimit` / `SnapshotAggregateLimit` / `SnapshotMaintenanceLimit` | `100` / `200` / `100` | Caps on the dashboard snapshot lists. |
| `AllowedOrigins` | `http://localhost:5173` | CORS origins. |
| `EnergyEuroPerKwh` | `0.18` | Energy tariff used for cost figures. |

Rate limits: `60 req/min` per user on command routes, `10 req/min` per IP on `/api/auth/*`.

> **Security:** `WarehouseAuth:SigningKey` must be at least 32 bytes, and the app refuses to start outside Development while the key still contains the demo value. Set it through an environment variable or a secret store in any real deployment.