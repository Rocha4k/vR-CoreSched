# Projeto vR-CoreSched - Blueprint Técnico

## Objetivo
Criar uma plataforma de monitorização e controlo de armazém em tempo real, com telemetria IoT simulada, motor de regras para alertas, mapa interativo de iluminação e analytics de consumo.

## Stack recomendada
- Backend: .NET 8 / ASP.NET Core
- Realtime: SignalR
- Mensageria IoT: MQTT com Mosquitto ou HiveMQ
- Persistência: PostgreSQL
- Cache e estado rápido: Redis
- Jobs em background: Hangfire ou Quartz
- Frontend: React + Vite + TypeScript
- Gráficos: ECharts ou Recharts
- Mapas/planta: SVG responsivo com hotspots clicáveis
- Observabilidade: OpenTelemetry + Serilog

## Arquitetura sugerida
### 1. Simulator Service
Serviço em background que simula máquinas e lâmpadas.
- Publica telemetria por MQTT a cada segundo.
- Gera estados de falha, picos e comportamento normal.
- Pode correr como Worker Service .NET para ficar simples de distribuir.

### 2. Ingestion API
Backend que subscreve tópicos MQTT e normaliza eventos.
- Valida payloads.
- Faz debounce e deduplicação.
- Escreve dados quentes numa tabela de eventos.
- Atualiza estado atual em cache para leitura rápida do frontend.

### 3. Rule Engine
Camada de análise em tempo real.
- Avalia regras por máquina e por janela temporal.
- Evita alertas repetidos com cooldown.
- Gera alertas críticos, médios e informativos.
- Persiste o alerta e publica o evento para SignalR.

### 4. Web UI
Painel de operação e gestão.
- Planta SVG com lâmpadas clicáveis.
- Lista de máquinas com estado live.
- Dashboard de consumo, alertas e custo mensal.
- Atualização em tempo real por SignalR.

### 5. Analytics Pipeline
Agregação periódica para histórico e relatórios.
- Job horário soma e média consumo por máquina, zona e período.
- Tabelas agregadas separadas para evitar consultas pesadas no detalhe.
- Índices por timestamp, máquina e zona.

Implementação atual:
- Relatórios mensais consumindo `consumption_aggregates`.
- Filtros por mês, máquina e zona no frontend.
- Exportação CSV e PDF a partir do backend.

## Melhorias que fazem sentido
### Performance
- Guardar telemetria de segundo a segundo só no curto prazo e agregar depois.
- Usar cache para estado atual das máquinas e lâmpadas.
- Separar tabela de eventos brutos de tabela de agregados.
- Evitar leituras diretas do frontend à tabela de eventos.

### Robustez
- Retry com backoff para MQTT e DB.
- Health checks para broker, base de dados e SignalR.
- Outbox pattern para comandos de iluminação, para não perder mensagens.
- Idempotência em eventos MQTT para evitar duplicados.

### Qualidade do produto
- Catálogo de máquinas com limites configuráveis por máquina.
- Modo demo com cenários pré-definidos: normal, stress, falha, manutenção.
- Histórico de alertas com acknowledge pelo operador.
- Trilho de auditoria para comandos de luz e alterações de regra.

### Segurança
- Autenticação com roles: Operador, Supervisor, Admin.
- Tópicos MQTT separados por domínio e permissões.
- Validação de payload e assinatura de comandos internos.
- Rate limit nas rotas de comando.

## Modelo de dados mínimo
### MachineTelemetry
- Id
- MachineId
- Timestamp
- Temperature
- Vibration
- Rpm
- EnergyKWh
- Source

### MachineState
- MachineId
- IsOnline
- LastSeen
- CurrentTemperature
- CurrentVibration
- CurrentRpm
- CurrentEnergy
- Severity

### Alert
- Id
- MachineId
- Severity
- RuleCode
- Message
- StartTime
- EndTime
- IsAcknowledged

### LightingDevice
- Id
- Zone
- Name
- IsOn
- LastChangedAt
- LastCommandSource

### ConsumptionAggregate
- Id
- PeriodStart
- PeriodEnd
- ScopeType
- ScopeId
- AvgKWh
- TotalKWh
- CostEuro

## Regras iniciais de negócio
- Temperatura > 85 e vibração acima do limiar durante 5 segundos gera alerta crítico.
- Máquina offline por mais de 10 segundos gera alerta de conectividade.
- Consumo acima da média da hora anterior por mais de 20 por cento gera alerta de eficiência.
- Luz ligada fora do horário de operação é sinalizada como desperdício.

## Tópicos MQTT sugeridos
- warehouse/machines/{machineId}/telemetry
- warehouse/machines/{machineId}/state
- warehouse/machines/{machineId}/alerts
- warehouse/lighting/{deviceId}/command
- warehouse/lighting/{deviceId}/state

## Estratégia de agregação
- Job a cada hora calcula médias, máximos e totais por máquina e zona.
- Job diário fecha custo total por turno e por área.
- Job mensal consolida relatórios para dashboard executivo.
- Consultas analíticas devem usar tabelas agregadas, não eventos brutos.

## Sugestão de estrutura do monorepo
- /backend
- /simulator
- /frontend
- /docs
- /infra

## Roadmap prático
1. Subir o backend com MQTT, SignalR e PostgreSQL.
2. Implementar simulator com 3 máquinas e 2 luzes.
3. Criar o motor de regras básico e alertas persistidos.
4. Montar o frontend com SVG e dados em tempo real.
5. Adicionar agregação horária e relatórios.
6. Introduzir observabilidade, auth e cenários demo.

## Decisão técnica recomendada
Se o objetivo é portfólio forte e execução rápida, eu avançaria com .NET 8 no backend e simulator, React no frontend, PostgreSQL para dados e Redis para estado atual. Isso reduz complexidade, mantém o sistema rápido e mostra uma arquitetura industrial credível.
