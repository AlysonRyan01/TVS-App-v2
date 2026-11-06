# Implementation Plan: Tela "Buscar OS"

**Branch**: `011-search-service-orders` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/011-search-service-orders/spec.md`

## Summary

Criar a tela `/buscar` (filtros no topo + tabela paginada) que busca OS por **um** filtro de cada
vez (número, cliente, modelo, série ou período de entrada) e abre um painel só de leitura com a OS.
No backend, uma rota REST `GET /service-orders/search` (handler sem MediatR, com as validações
testadas) substitui as quatro rotas antigas de busca por campo, que estão quebradas. Sem migration.
Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22 (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core (SQL Server), Angular Material (form-field, datepicker, table, paginator, dialog)

**Storage**: SQL Server, sem mudança de schema

**Testing**: MSTest + fakes à mão (handler); frontend por build e roteiro manual

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: resultado em < 2 s para um mês de OS (consulta projetada, paginada em 20)

**Constraints**: um filtro por vez; REST; sem MediatR; pt-BR; pacote inicial < 1 MB

**Scale/Scope**: 1 rota nova, 4 removidas; 1 método de handler + 1 de repositório; 1 página, 1 diálogo, 1 serviço

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Filtro e projeção no Domain; regras no handler (Application); consulta na Infrastructure; endpoint só compõe. |
| II. Domain isolado | ✅ | Só records novos, sem dependências. |
| III. EF Core + migrations | ✅ | Nenhuma mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes de `Search` em `ServiceOrderQueryHandlerTests` (um filtro por vez, cliente ≥ 3, datas, paginação, normalização). |
| V. UI em português | ✅ | Rótulos e mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/011-search-service-orders/
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
├── TVSApp.Domain/Repositories/
│   ├── Responses.cs                                       # + ServiceOrderSearchItem
│   └── ServiceOrders/
│       ├── ServiceOrderSearchFilter.cs                    # NOVO
│       └── IServiceOrderQueryRepository.cs                # + SearchAsync; − 4 métodos antigos sem uso
├── TVSApp.Application/
│   ├── Requests/ServiceOrderRequests/ServiceOrdersQueries/
│   │   ├── ServiceOrderSearchQuery.cs                     # NOVO
│   │   └── GetServiceOrdersBy{CustomerName,SerialNumber,Model,Date}Query.cs  # REMOVER
│   └── Handlers/IServiceOrderQueryHandler.cs, ServiceOrderQueryHandler.cs    # + Search
├── TVSApp.Infrastructure/Repositories/ServiceOrders/ServiceOrderQueryRepository.cs  # + SearchAsync; − 4 antigos
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs          # + GET /service-orders/search; − 4 rotas
└── TVSApp.Tests/Handlers/
    ├── ServiceOrderQueryHandlerTests.cs                   # + testes de Search
    └── Fakes/FakeServiceOrderQueryRepository.cs           # SearchAsync (guarda o filtro)

web/src/app/
├── app.routes.ts                                          # /buscar (loadComponent)
├── shared/layout/base-layout/base-layout.component.ts     # "Buscar OS" → /buscar
└── features/search-service-orders/                        # NOVO
    ├── search-service-orders.component.{ts,html,scss}
    ├── search-service-orders.service.ts
    ├── service-order-details-dialog/service-order-details-dialog.component.{ts,html,scss}
    └── models/service-order-search.model.ts
```

**Structure Decision**: página nova em `features/search-service-orders/`, irmã das outras telas.

### Ordem de implementação sugerida

1. Domain: filtro + projeção; interface do repositório.
2. Testes de `Search` + fake; handler; repositório; rota; remoção das antigas.
3. Frontend: serviço e modelos; página (filtros + tabela); diálogo de detalhes; rota e menu.
4. Quickstart.

## Complexity Tracking

Nenhuma violação. Observação: a regra "um filtro por vez" é aplicada na tela (limpa os outros) e
validada no servidor.
