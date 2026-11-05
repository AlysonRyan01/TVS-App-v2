# Implementation Plan: Padronização de nomenclatura das filas da tela Home

**Branch**: `001-home-naming-consistency` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-home-naming-consistency/spec.md`

## Summary

Renomear de ponta a ponta (rota → handler → repositório → record → serviço/modelo/componente
Angular) as 6 filas da Home e o modal de orçamento, seguindo o glossário da spec
(`PendingEstimate`, `AwaitingResponse`, `Approved`, `Rejected`, `AwaitingPart`,
`ReadyForPickup`, `Estimate`). Os campos de data passam a terminar em `At` e `Guarantee` vira
`Warranty` no contrato do orçamento.

Abordagem técnica (pedida pelo usuário): **sem MediatR** no escopo. As filas usam o
`IServiceOrderQueryHandler` que já existe. O orçamento passa para um novo
`IServiceOrderCommandHandler` (handler comum com interface, registrado no DI). Todos os
endpoints retornam `BaseResponse<T>.ToResult()`. Isso também corrige um defeito encontrado: a
rota `add-service-order-estimate` está sem handler desde o commit `0f7f7d3`
([research.md](research.md) R2).

## Technical Context

**Language/Version**: C# / .NET 9 (`net9.0`); TypeScript / Angular (standalone components, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, SignalR (`ServiceOrderHub`), Angular Material (tabs, table, paginator, dialog). MediatR continua no projeto para outros módulos, mas não é usado no escopo.

**Storage**: SQL Server via EF Core. Sem alterações de schema.

**Testing**: MSTest em `src/TVSApp.Tests`, com fakes escritos à mão. Build do Angular (`ng build`).

**Target Platform**: API Linux (Docker) + SPA no navegador

**Project Type**: web application (backend .NET em `src/` + frontend Angular em `web/`)

**Performance Goals**: N/A (refatoração; as consultas não mudam)

**Constraints**: comportamento visível idêntico (FR-009); frontend e API publicados juntos; sem migration

**Scale/Scope**: 6 rotas GET + 1 PUT; ~4 arquivos .NET de produção + 1 novo handler; ~12 arquivos Angular; 2 classes de teste novas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Handlers e command na Application; records e interfaces de repositório no Domain; implementação na Infrastructure; endpoint na Api só faz a composição (handler → `ToResult()`, hub). |
| II. Domain isolado | ✅ | Só os nomes de records e de métodos de interface mudam no Domain. Nenhuma dependência nova. |
| III. EF Core + migrations | ✅ | Nenhuma mudança de schema. `Guarantee` continua na entidade e no banco (FR-011). |
| IV. Regras de negócio testadas | ✅ | O `ServiceOrderCommandHandler.AddEstimate` é recriado → testes em `TVSApp.Tests` (R6). As regras do domínio não mudam. |
| V. UI em português | ✅ | Rótulos, colunas e mensagens não mudam. Só os identificadores mudam. |
| Quality gates | ✅ | `dotnet build`, `dotnet test` e `ng build` no [quickstart.md](quickstart.md). |

**Post-design re-check**: ✅ sem violações. A Complexity Tracking não se aplica.

## Project Structure

### Documentation (this feature)

```text
specs/001-home-naming-consistency/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── http-endpoints.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── TVSApp.Domain/
│   └── Repositories/
│       ├── Responses.cs                                   # renomear records e campos
│       └── ServiceOrders/IServiceOrderQueryRepository.cs  # renomear métodos das filas
├── TVSApp.Application/
│   ├── Handlers/
│   │   ├── IServiceOrderQueryHandler.cs                   # renomear métodos das filas
│   │   ├── ServiceOrderQueryHandler.cs
│   │   ├── IServiceOrderCommandHandler.cs                 # NOVO: AddEstimate
│   │   └── ServiceOrderCommandHandler.cs                  # NOVO (sem MediatR)
│   └── Requests/ServiceOrderRequests/ServiceOrderCommands/
│       └── AddServiceOrderEstimateCommand.cs              # remove IRequest; Guarantee → Warranty
├── TVSApp.Infrastructure/
│   └── Repositories/ServiceOrders/ServiceOrderQueryRepository.cs  # renomear métodos e projeções
├── TVSApp.Api/
│   ├── Common/BuilderExtensions.cs                        # AddScoped<IServiceOrderCommandHandler, ...>
│   └── Endpoints/ServiceOrderEndpoints.cs                 # 6 rotas renomeadas + PUT sem ISender
└── TVSApp.Tests/
    └── Handlers/
        ├── Fakes/                                         # NOVO: repositórios e fila fake
        ├── ServiceOrderCommandHandlerTests.cs             # NOVO
        └── ServiceOrderQueryHandlerTests.cs               # NOVO

web/src/app/features/home/
├── home.component.{ts,html}                               # signals, handlers, onTabChange
├── home.service.ts                                        # métodos e rotas
├── models/
│   ├── pending-estimate-service-order.interface.ts        # antes: ...-ordens.interface.ts
│   ├── awaiting-response-service-order.interface.ts       # antes: waiting-response-...
│   ├── approved-service-order.interface.ts
│   ├── rejected-service-order.interface.ts                # antes: refused-...
│   ├── awaiting-part-service-order.interface.ts           # antes: waiting-part-...
│   ├── ready-for-pickup-service-order.interface.ts        # antes: ready-...
│   └── table-pagination.interface.ts
└── modals/estimate/                                       # antes: modals/quote/
    ├── add-service-order-estimate-modal.component.{ts,html,scss}
    ├── add-service-order-estimate-modal.service.ts
    └── models/add-service-order-estimate.model.ts         # guarantee → warranty
```

**Structure Decision**: web application já existente. O backend fica em `src/` (4 camadas +
testes) e o frontend em `web/`. O único arquivo novo de produção é o command handler. O resto
são renomeações dentro dos arquivos atuais, mais a troca de nome de arquivos do Angular para o
padrão singular `<termo>-service-order.interface.ts`.

### Ordem de implementação sugerida

1. **Domain**: records e interface de repositório. Faz o build quebrar e mostra tudo que precisa mudar.
2. **Infrastructure**: repositório de consulta (nomes e projeções).
3. **Application**: query handler renomeado, command sem `IRequest`, novo command handler.
4. **Api**: registro no DI e endpoints (6 GET + PUT).
5. **Testes**: fakes e testes dos dois handlers.
6. **Angular**: modelos e arquivos, serviço, componente e template, modal `estimate`.
7. **Validação** pelo [quickstart.md](quickstart.md).

## Complexity Tracking

Nenhuma violação da constituição. Seção não aplicável.
