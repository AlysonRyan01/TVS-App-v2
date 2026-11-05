# Implementation Plan: Modal "Consertado" na aba "Aguardando peça"

**Branch**: `005-repair-modal` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-repair-modal/spec.md`

## Summary

Adicionar à aba "Aguardando peça" a coluna **Ação** com o botão **CONSERTADO**. O botão abre o
modal "Conserto" (mesmo design dos outros) com o botão **Consertado**, que registra o conserto,
move a OS para "Prontos" e avisa o cliente pelo WhatsApp.

No backend, a rota quebrada `add-service-order-repair` (MediatR sem handler) dá lugar a
`PUT /service-orders/{id}/repair`. A lógica fica em `IServiceOrderCommandHandler.RegisterRepair`,
reproduzindo exatamente `old-handlers/AddServiceOrderRepairHandler.cs` ([research.md](research.md) R1).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, SignalR, fila de WhatsApp, Angular Material

**Storage**: SQL Server via EF Core. Sem alteração de schema.

**Testing**: MSTest + fakes escritos à mão (`src/TVSApp.Tests`)

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: N/A

**Constraints**: sem MediatR; `ToResult()`; UI em pt-BR; fluxo idêntico ao handler antigo

**Scale/Scope**: 1 rota nova e 1 removida; 1 método de handler; 1 modal novo (4 arquivos); ajuste na Home

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Orquestração no handler; regra no Domain (método existente); endpoint só faz composição. |
| II. Domain isolado | ✅ | Sem mudança no Domain. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes de handler e de domínio (R4). |
| V. UI em português | ✅ | "CONSERTADO", "Conserto", "Consertado", "Cancelar", mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/005-repair-modal/
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
│   ├── Handlers/IServiceOrderCommandHandler.cs         # + RegisterRepair(long id)
│   ├── Handlers/ServiceOrderCommandHandler.cs          # implementação (= handler antigo)
│   └── Requests/.../ServiceOrderCommands/AddServiceOrderRepairCommand.cs   # REMOVER
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs       # rota REST nova; remove a antiga
└── TVSApp.Tests/
    ├── Domain/ServiceOrderRepairTests.cs               # NOVO
    └── Handlers/ServiceOrderCommandHandlerTests.cs     # + testes de RegisterRepair

web/src/app/features/home/
├── home.component.{ts,html}                            # coluna action + CONSERTADO + openRepairDialog
└── modals/repair/                                      # NOVO
    ├── repair-modal.component.{ts,html,scss}
    └── repair-modal.service.ts
```

**Structure Decision**: o mesmo padrão das features 002 a 004.

## Complexity Tracking

Nenhuma violação. Observações:
- Não checar o resultado do salvamento antes de enviar o WhatsApp é intencional (fidelidade ao
  handler antigo) e fica documentado num teste.
- São 5 modais repetindo o mesmo painel lateral e os mesmos estilos; a extração continua
  recomendada.
