# vR-CoreSched

Plataforma de monitorização e controlo de armazém com telemetria IoT simulada, MQTT, motor de regras em tempo real, iluminação interativa e analytics de consumo.

[![.NET Core](https://img.shields.io/badge/.NET%20Core-512BD4?style=flat-square&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-61DAFB?style=flat-square&logo=react&logoColor=black)](https://react.dev/)
[![MQTT](https://img.shields.io/badge/MQTT-3C525C?style=flat-square&logo=eclipse-mosquitto&logoColor=white)](https://mqtt.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)

---

## Overview

O **vR-CoreSched** é uma solução integrada para a gestão, monitorização e automatização de armazéns industriais. Através da ingestão de telemetria via MQTT e processamento em tempo real com um motor de regras dinâmico, a plataforma permite acompanhar o estado operacional de máquinas, gerir alertas críticos e controlar sistemas de iluminação diretamente sobre uma planta interativa.

O foco principal é a eficiência operacional e a rapidez de resposta, consolidando num único ecossistema desde a telemetria bruta de sensores até à agregação de dados para relatórios analíticos de consumo e custos.

---

## Módulos do Sistema

* **Operação em Tempo Real** — Ingestão contínua de telemetria, disparo instantâneo de alertas e controlo reativo de iluminação.
* **Administração de Regras e Equipamentos** — Gestão dinâmica de regras de negócio, máquinas e zonas operacionais com persistência robusta.
* **Planta Interativa SVG** — Visualização dinâmica do armazém baseada em SVG, com contornos editáveis e hotspots reposicionáveis diretamente na interface.
* **Analytics e Agregação** — Processamento e agregação horária de consumo de energia para geração acelerada de relatórios.
* **Gestão de Alertas e Manutenção** — Sistema de *acknowledge* (reconhecimento) de incidentes com criação automática de histórico para auditoria e manutenção.
* **Segurança e Perfis** — Autenticação via JWT com suporte a refresh tokens rotativos e três níveis de acesso: Operador, Supervisor e Admin.
* **Reporting Avançado** — Filtros avançados por mês, máquina e zona, com suporte à exportação de dados em formatos CSV e PDF.

---

## Objetivo do MVP

1. **Simulação Industrial:** Simular o comportamento de máquinas reais gerando dados de temperatura, vibração, RPM e consumo.
2. **Ingestão em Tempo Real:** Capturar telemetria via broker MQTT e processar alertas críticos instantaneamente.
3. **Controlo Visual:** Disponibilizar uma interface web para controlo de iluminação sobre a planta SVG com atualizações em tempo real (SignalR).
4. **Eficiência de Dados:** Agregar consumo energético por hora para produzir relatórios de custo otimizados.

---

## Tech Stack

* **Backend:** ASP.NET Core com SignalR (WebSockets) e cliente MQTT.
* **Simulador:** Worker Service em .NET para simulação concorrente de carga industrial.
* **Frontend:** React, Vite e TypeScript para uma SPA rápida e tipada.
* **Cache & Message Broker:** Redis para estado quente e Mosquitto/Broker para mensagens MQTT.
* **Base de Dados:** PostgreSQL para persistência relacional e dados históricos.
* **Infraestrutura:** Docker e Docker Compose para orquestração local do ambiente.

---

## Como Correr

### 1. Subir a Infraestrutura (Docker)
Certifica-te de que tens o Docker instalado e executa o seguinte comando na raiz do projeto onde se encontra o `docker-compose.yml`:
```bash
docker-compose up -d