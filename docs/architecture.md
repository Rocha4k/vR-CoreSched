# Arquitetura

## Fluxo principal

1. O simulador gera leituras por máquina e por lâmpada.
2. As leituras são publicadas em tópicos MQTT.
3. O backend subscreve os tópicos, normaliza os eventos e grava o estado quente.
4. O motor de regras avalia anomalias e cria alertas.
5. O SignalR empurra alterações para o frontend em tempo real.
6. Um job de agregação consolida consumo por hora e custo mensal.

## Decisões para manter o sistema rápido

- O frontend lê snapshots agregados e nunca varre telemetria bruta.
- A telemetria é guardada com retenção curta e agregada periodicamente.
- Os comandos de luz usam um fluxo idempotente com confirmação de estado.
- Alertas críticos têm cooldown para não duplicar notificações.

## Extensões futuras

- Autenticação com roles e auditoria de comandos.
- Painel de manutenção com previsões simples de falha.
- Exportação CSV e relatórios mensais automáticos.
- Modelos de previsão de consumo com histórico agregado.
