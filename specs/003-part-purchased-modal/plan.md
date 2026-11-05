# Implementation Plan: Modal "Comprado" na aba "Aprovados"

**Branch**: `003-part-purchased-modal` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-part-purchased-modal/spec.md`

## Summary

Adicionar à aba "Aprovados" a coluna **Ação** com o botão **COMPRADO**. O botão abre o modal
"Compra da peça" (mesmo padrão do modal de resposta) com o botão **Comprado**, que registra a
compra da peça e move a OS para "Aguardando peça".

No backend, a rota quebrada `add-service-order-purchased-part` (MediatR sem handler) é trocada
por `PUT /service-orders/{id}/part/purchase`. A lógica fica em
`IServiceOrderCommandHandler.RegisterPartPurchase`, reproduzindo exatamente
`old-handlers/AddPurchasedPartHandler.cs`.

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, SignalR, Angular Material

**Storage**: SQL Server via EF Core. Sem alteração de schema.

**Testing**: MSTest + fakes escritos à mão (`src/TVSApp.Tests`)

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: N/A

**Constraints**: sem MediatR; `ToResult()`; UI em pt-BR; comportamento idêntico ao handler antigo

**Scale/Scope**: 1 rota nova e 1 removida; 1 método de handler; 1 modal novo (4 arquivos); ajuste na Home

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Handler na Application, regra no Domain (método existente), endpoint só faz composição. |
| II. Domain isolado | ✅ | Sem mudança no Domain. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes do handler e de `AddPurchasedPart()` (R4). |
| V. UI em português | ✅ | "COMPRADO", "Compra da peça", "Comprado", "Cancelar", mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/003-part-purchased-modal/
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
│   ├── Handlers/IServiceOrderCommandHandler.cs         # + RegisterPartPurchase(long id)
│   ├── Handlers/ServiceOrderCommandHandler.cs          # implementação (= handler antigo)
│   └── Requests/.../ServiceOrderCommands/AddPurchasedPartCommand.cs   # REMOVER
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs       # rota REST nova; remove a antiga
└── TVSApp.Tests/
    ├── Domain/ServiceOrderPartPurchaseTests.cs         # NOVO
    └── Handlers/ServiceOrderCommandHandlerTests.cs     # + testes de RegisterPartPurchase

web/src/app/features/home/
├── home.component.{ts,html}                            # coluna action + COMPRADO + openPartPurchaseDialog
└── modals/part-purchase/                               # NOVO
    ├── part-purchase-modal.component.{ts,html,scss}
    └── part-purchase-modal.service.ts
```

**Structure Decision**: o mesmo padrão das features 001 e 002. O modal novo fica ao lado de
`modals/estimate/` e `modals/estimate-response/`.

## Complexity Tracking

Nenhuma violação. Observação: a estrutura do modal (ticket e estilos) passa a se repetir em 3
modais. Extrair isso fica como sugestão para uma feature de refatoração (R3).
