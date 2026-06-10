# Fluxo da Interface

## Operação

- Mostra telemetria, alertas, consumo e controlo rápido das luzes.
- Recebe eventos em tempo real através de SignalR.
- Permite fazer acknowledge de alertas críticos e ver o histórico de manutenção.

## Administração

- Permite ajustar regras, máquinas e zonas sem editar código.
- As alterações são persistidas em PostgreSQL.
- O acesso é controlado por role, com Supervisor e Admin a editar estrutura e Admin a editar regras.

## Planta

- Usa uma base SVG com textura comum para todos os armazéns.
- O contorno da planta é desenhado por pontos.
- Os hotspots de luz e de máquina podem ser reposicionados por drag and drop.
- A posição dos hotspots é guardada na base de dados.

## Analytics

- O gráfico mensal junta consumo e custo a partir das agregações da base de dados.
- O objetivo é evitar consultas ao detalhe segundo a segundo no dashboard.
- O ecrã de reporting permite filtrar por mês, máquina e zona e exportar CSV ou PDF.

## Perfis

- Cada utilizador pode atualizar o seu nome e password.
- O Admin tem também a gestão de utilizadores reais com ativação, role e criação de novas contas.
