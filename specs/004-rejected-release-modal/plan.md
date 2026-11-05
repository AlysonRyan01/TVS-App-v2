# Implementation Plan: Modal "Liberado" na aba "Reprovados"

**Branch**: `004-rejected-release-modal` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-rejected-release-modal/spec.md`

## Summary

Adicionar à aba "Reprovados" a coluna **Ação** com o botão **LIBERADO**. O botão abre o modal
"Liberação" (mesmo design dos outros) com o botão **Liberado**, que marca a OS reprovada como
"Pronto para entrega" e avisa o cliente pelo WhatsApp.

No backend, a rota quebrada `mark-estimate-rejected-as-ready-to-pick-up` (MediatR sem handler)
dá lugar a `PUT /service-orders/{id}/ready-for-pickup`. A lógica fica em
`IServiceOrderCommandHandler.MarkRejectedEstimateAsReadyForPickup`, reproduzindo exatamente
`old-handlers/MarkEstimateRejectedAsReadyToPickUpHandler.cs` ([research.md](research.md) R1).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, SignalR, fila de WhatsApp, Angular Material

**Storage**: SQL Server via EF Core. Sem alteração de schema.

**Testing**: MSTest + fakes escritos à mão (`src/TVSApp.Tests`)

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: N/A

**Constraints**: sem MediatR; `ToResult()`; UI em pt-BR; fluxo idêntico ao handler antigo

**Scale/Scope**: 1 rota nova e 1 removida; 1 método de handler; ajuste em 1 fake; 1 modal novo (4 arquivos); ajuste na Home

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Orquestração no handler da Application; regra no Domain (método existente); endpoint só faz composição. |
| II. Domain isolado | ✅ | Sem mudança no Domain. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Um teste para cada saída do fluxo, mais testes de domínio do método existente (R4). |
| V. UI em português | ✅ | "LIBERADO", "Liberação", "Liberado", "Cancelar", mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/004-rejected-release-modal/
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
│   ├── Handlers/IServiceOrderCommandHandler.cs         # + MarkRejectedEstimateAsReadyForPickup(long id)
│   ├── Handlers/ServiceOrderCommandHandler.cs          # implementação (= handler antigo)
│   └── Requests/.../ServiceOrderCommands/MarkEstimateRejectedAsReadyToPickUpCommand.cs   # REMOVER
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs       # rota REST nova; remove a antiga
└── TVSApp.Tests/
    ├── Domain/ServiceOrderRejectedReleaseTests.cs      # NOVO
    └── Handlers/
        ├── Fakes/FakeServiceOrderCommandRepository.cs  # + UpdateResponse configurável
        ├── Fakes/FakeServiceOrderQueryRepository.cs    # + flag GetByIdForUpdateCalled
        └── ServiceOrderCommandHandlerTests.cs          # + testes de liberação

web/src/app/features/home/
├── home.component.{ts,html}                            # coluna action + LIBERADO + openRejectedReleaseDialog
└── modals/rejected-release/                            # NOVO
    ├── rejected-release-modal.component.{ts,html,scss}
    └── rejected-release-modal.service.ts
```

**Structure Decision**: o mesmo padrão das features 002 e 003.

## Complexity Tracking

Nenhuma violação. Observações:
- Com este, são 4 modais com o mesmo painel lateral e os mesmos estilos. A extração para um
  componente compartilhado continua recomendada como feature de refatoração.
- A falta de proteção contra liberar duas vezes é intencional, para seguir o handler antigo
  (ver Edge Cases da spec).
