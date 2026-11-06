# Implementation Plan: Funções gerais

**Branch**: `012-general-functions` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/012-general-functions/spec.md`

## Summary

Levar as funções da antiga tela "Geral" para onde a OS já está aberta: o painel da **Buscar OS**
ganha **Reimprimir** (`GET /service-orders/{id}/pdf`, seguindo o handler antigo), **Alterar**
(`PUT /service-orders/{id}`, campos da OS sem cliente nem situação) e a edição rápida da
**Localização** (rota existente). O menu troca "Geral" por **Clientes** (`/clientes`: busca + editar
com o modal de cliente existente). Sem migration. Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22 (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, QuestPDF (`RegeneratePdfAsync`), SignalR, Angular Material

**Storage**: SQL Server, sem mudança de schema

**Testing**: MSTest + fakes (handlers); frontend por build e roteiro manual

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: n/a

**Constraints**: seguir os handlers antigos (com as diferenças do R1); REST; sem MediatR; pt-BR; inicial < 1 MB

**Scale/Scope**: 2 métodos de handler + testes; 2 rotas novas e 2 removidas; painel com modo edição; 1 página nova; ajuste no modal de cliente; menu

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Regras no handler (Application); entidade com métodos existentes; endpoints só compõem. |
| II. Domain isolado | ✅ | Domain não muda. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes de `UpdateServiceOrder` (validações, campos aplicados, localização preservada, venda preservada, cliente/situação intocados) e `RegeneratePdf` (404, falha, sucesso). |
| V. UI em português | ✅ | Rótulos e mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/012-general-functions/
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
│   ├── Requests/ServiceOrderRequests/ServiceOrderCommands/
│   │   ├── EditServiceOrderCommand.cs                    # reescrito (sem MediatR, sem cliente/situação)
│   │   └── RegenerateAndReturnPdfCommand.cs              # REMOVER
│   ├── Handlers/IServiceOrderCommandHandler.cs, ServiceOrderCommandHandler.cs   # + UpdateServiceOrder, RegeneratePdf
│   ├── Mappers/ServiceOrderMapper.cs                     # − ApplyChanges, MapToUpdateResponseDto
│   └── DTOs/ServiceOrder/UpdateServiceOrderResponseDto.cs  # REMOVER
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs         # + PUT /service-orders/{id}, GET /service-orders/{id}/pdf; − 2 antigas
└── TVSApp.Tests/Handlers/
    ├── ServiceOrderCommandHandlerTests.cs                # + testes
    └── Fakes/FakeServiceOrderPdfGenerator.cs             # RegenerateResponse / RegenerateCalls

web/src/app/
├── app.routes.ts                                         # /clientes (loadComponent)
├── shared/layout/base-layout/base-layout.component.ts    # − Geral; + Clientes
├── features/customers/
│   ├── customer-dialog/customer-dialog.component.{ts,html}   # mode 'edit'
│   └── customers-page/customers-page.component.{ts,html,scss} # NOVO
└── features/search-service-orders/
    ├── search-service-orders.service.ts                  # + update, regeneratePdf, updateLocation
    ├── search-service-orders.component.ts                # atualiza a linha após alterar
    └── service-order-details-dialog/                     # form + modo edição + Reimprimir + Localização
```

**Structure Decision**: ações no painel existente; a página de clientes fica junto dos componentes
de cliente em `features/customers/`.

### Ordem de implementação sugerida

1. Testes + `UpdateServiceOrder` + `RegeneratePdf`; rotas; remoção das antigas.
2. Painel: Reimprimir e Localização (rápidos), depois o modo edição.
3. Clientes: modo editar no modal, página, rota e menu.
4. Quickstart.

## Complexity Tracking

Nenhuma violação. O painel passa a ter dois modos (leitura/edição); o resto reaproveita o que já
existe.
