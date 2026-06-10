<div align="center">

<img src="static/img/vR-Clean.png" alt="vR-CoreSched" width="220">

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

### 1. Spin up the Infrastructure (Docker)
Ensure Docker is installed and running, then execute the following command in the project root (where `docker-compose.yml` is located):
```bash
docker-compose up -d