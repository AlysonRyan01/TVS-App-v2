# Implementation Plan: Tela "Métricas"

**Branch**: `013-metrics-dashboard` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/013-metrics-dashboard/spec.md`

## Summary

Criar a tela `/metricas` igual à imagem do sistema antigo: período (Mês/Ano ou De/Até), 11
cartões de indicadores, 2 gráficos de rosca e a tabela de produtos entregues com o valor da peça
editável. No backend, o cálculo que já existe sai do `MetricsService` para um calculador puro e
testado na Application (com a correção do tempo de ciclo negativo e os campos Valor total / Total
líquido), as rotas de métricas viram `GET /metrics` (sem MediatR) e o valor da peça ganha
`PUT /service-orders/{id}/part/purchase-cost`. Sem migration. Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22 (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, Angular Material (select, form-field, table); gráficos em SVG próprio (sem biblioteca)

**Storage**: SQL Server, sem mudança de schema

**Testing**: MSTest + fakes (calculador, handler de métricas, valor da peça); frontend por build e roteiro manual

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: tela em < 3 s para um mês

**Constraints**: mesmas definições do sistema antigo; REST; sem MediatR; pt-BR; inicial < 1 MB

**Scale/Scope**: 1 calculador + 1 handler + 1 método no command handler; 2 rotas novas e 3 removidas; 1 página, 1 componente de gráfico, 1 serviço

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Cálculo na Application (`DashboardMetricsCalculator`); consultas na Infrastructure (`MetricsService`); handlers sem MediatR; endpoints só compõem. |
| II. Domain isolado | ✅ | Domain não muda. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | `DashboardMetricsCalculatorTests` (cada indicador, vendas separadas, ciclo negativo, totais), `MetricsQueryHandlerTests` (período) e testes de `UpdatePartPurchaseCost`. |
| V. UI em português | ✅ | Rótulos e mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/013-metrics-dashboard/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/http-endpoints.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── TVSApp.Application/
│   ├── Services/DashboardMetricsCalculator.cs              # NOVO (BuildMetrics extraído + ajustes)
│   ├── DTOs/Dashboard/DashboardMetricsDto.cs               # + TotalValue, NetTotal
│   ├── DTOs/Dashboard/DeliveredProductDto.cs               # + ProductLabel
│   ├── Handlers/Metrics/IMetricsQueryHandler.cs, MetricsQueryHandler.cs   # NOVO
│   ├── Handlers/Metrics/GetDashboardMetrics{,ByRange}Handler.cs           # REMOVER (MediatR)
│   ├── Requests/MetricsRequests/GetDashboardMetrics{,ByRange}Query.cs     # REMOVER
│   ├── Requests/ServiceOrderRequests/ServiceOrderCommands/UpdatePartPurchaseCostCommand.cs  # REMOVER
│   └── Handlers/IServiceOrderCommandHandler.cs, ServiceOrderCommandHandler.cs  # + UpdatePartPurchaseCost
├── TVSApp.Infrastructure/Services/MetricsService.cs        # consulta + calculador
├── TVSApp.Api/
│   ├── Endpoints/DashboardEndpoints.cs                      # GET /metrics; − 2 rotas
│   ├── Endpoints/ServiceOrderEndpoints.cs                   # PUT /service-orders/{id}/part/purchase-cost; − 1 rota
│   └── Common/BuilderExtensions.cs                          # DI do IMetricsQueryHandler
└── TVSApp.Tests/
    ├── Application/DashboardMetricsCalculatorTests.cs      # NOVO
    ├── Handlers/MetricsQueryHandlerTests.cs                 # NOVO (+ FakeMetricsService)
    └── Handlers/ServiceOrderCommandHandlerTests.cs          # + UpdatePartPurchaseCost

web/src/app/
├── app.routes.ts                                            # /metricas (loadComponent)
├── shared/layout/base-layout/base-layout.component.ts       # "Métricas" → /metricas
└── features/metrics/                                        # NOVO
    ├── metrics.component.{ts,html,scss}
    ├── metrics.service.ts
    ├── donut-chart/donut-chart.component.{ts,html,scss}
    └── models/dashboard-metrics.model.ts
```

**Structure Decision**: página em `features/metrics/`, com o gráfico como subcomponente.

### Ordem de implementação sugerida

1. Calculador + testes; `MetricsService` usando o calculador.
2. Handler de métricas + testes; `GET /metrics`; remoção das rotas MediatR.
3. Valor da peça: testes, handler, rota; remoção da antiga.
4. Frontend: serviço/modelos, donut (seguindo o skill dataviz), página, rota e menu.
5. Quickstart.

## Complexity Tracking

Nenhuma violação. O `BuildMetrics` muda de lugar (Infrastructure → Application) só para ser
testável; o comportamento é o mesmo, exceto a correção do ciclo e os 2 campos novos.
