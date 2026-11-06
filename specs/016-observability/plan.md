# Implementation Plan: Observabilidade (logs, métricas e painéis)

**Branch**: `016-observability` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/016-observability/spec.md`

## Summary

A API passa a escrever logs estruturados em JSON (Serilog) com correlação por requisição, eventos
de negócio e o ciclo do WhatsApp (agora conferindo o resultado de cada envio), e expõe métricas
internas. Na VPS, o Grafana Alloy coleta os logs dos containers (→ Loki) e as métricas da API, da
VPS e dos containers (→ Prometheus); o Grafana, publicado num subdomínio com HTTPS pelo Caddy, traz
4 painéis e as regras de alerta prontas (sem envio). Tudo sobe com o mesmo `docker compose up`.
Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9; Docker Compose; configs YAML/River (Alloy); dashboards JSON

**Primary Dependencies**: Serilog.AspNetCore, Serilog.Formatting.Compact, prometheus-net.AspNetCore; imagens grafana/loki, prom/prometheus, grafana/grafana, grafana/alloy

**Storage**: volumes Docker (Loki 30 dias, Prometheus 15 dias); banco do sistema segue no Azure

**Testing**: MSTest (mascaramento, catálogo de eventos); validação do compose; roteiro manual

**Target Platform**: VPS Linux com Docker

**Project Type**: web application + infraestrutura

**Performance Goals**: sobrecarga desprezível na API; pilha de observabilidade ≤ ~1,5 GB de RAM

**Constraints**: API não depende da pilha (FR-010); nada exposto além do Grafana via Caddy; sem dados pessoais nos logs

**Scale/Scope**: 3 pacotes NuGet; 2 middlewares/filtros + 1 helper; ajuste no worker e na fila do WhatsApp; 4 serviços novos no compose; 4 painéis; 4 regras de alerta

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Serilog/Prometheus só na Api/Infrastructure; Application só ganha constantes e `LogMask` (sem dependências novas). |
| II. Domain isolado | ✅ | Domain não muda. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | `LogMaskTests` e testes do catálogo de eventos; o envio de WhatsApp não muda. |
| V. UI em português | ✅ | Mensagens de log, painéis e alertas em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/016-observability/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/http-and-deploy.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── TVSApp.Application/Observability/{BusinessEvents.cs, LogMask.cs}       # NOVO
├── TVSApp.Api/
│   ├── Common/ObservabilityExtensions.cs                                    # NOVO: Serilog, métricas, request logging
│   ├── Middlewares/CorrelationIdMiddleware.cs                               # NOVO
│   ├── Observability/BusinessEventFilter.cs                                 # NOVO (+ .WithBusinessEvent)
│   ├── Endpoints/**                                                         # .WithBusinessEvent(...) nas rotas de comando
│   └── Program.cs                                                           # liga tudo
├── TVSApp.Infrastructure/Messaging/{Queue/WhatsAppQueue.cs, BackgroundWorker/WhatsAppNotificationWorker.cs}  # logs + contador
└── TVSApp.Tests/Application/{LogMaskTests.cs, BusinessEventsTests.cs}     # NOVO

observability/                                                               # NOVO
├── alloy/config.alloy
├── loki/loki.yaml
├── prometheus/prometheus.yml
└── grafana/provisioning/{datasources,dashboards,alerting}/…  + dashboards/*.json

docker-compose.yml   # + loki, prometheus, grafana, alloy
web/Caddyfile        # + bloco do Grafana; /api/metrics → 404
.env.example, DEPLOY.md
```

**Structure Decision**: configs de infraestrutura em `observability/` na raiz, montadas nos
containers; código de observabilidade da API concentrado em `Common/ObservabilityExtensions.cs`.

### Ordem de implementação sugerida

1. `LogMask` + `BusinessEvents` com testes.
2. Serilog, correlação, request logging, métricas na API.
3. Filtro de eventos nas rotas; WhatsApp (fila e worker).
4. Configs de Loki, Prometheus, Alloy; compose; Caddy.
5. Grafana: datasources, painéis, alertas; `.env`/DEPLOY.
6. Quickstart (local com Docker).

## Complexity Tracking

Nenhuma violação. O worker do WhatsApp muda só no que registra (passa a conferir o resultado de
cada envio para logar falhas); o comportamento de envio é o mesmo.
