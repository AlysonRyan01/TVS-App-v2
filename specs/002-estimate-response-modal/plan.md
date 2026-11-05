# Implementation Plan: Modal de resposta do orçamento

**Branch**: `002-estimate-response-modal` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-estimate-response-modal/spec.md`

## Summary

Adicionar à aba "Aguardando resposta" a coluna **Ação** com o botão **RESPOSTA**, que abre um
modal no padrão do modal de orçamento para registrar **Aprovado** ou **Reprovado**.

No backend:
- as rotas quebradas (MediatR sem handler) são substituídas por
  `PUT /service-orders/{id}/estimate/approve|reject`, com login obrigatório;
- a lógica fica em `IServiceOrderCommandHandler.ApproveEstimate/RejectEstimate`, sem MediatR;
- a regra "só responde quem está aguardando resposta" fica no Domain;
- os efeitos que existiam são mantidos (notificação interna, SignalR) e o WhatsApp volta a ser
  enviado, exceto para PhilcoBritania.

Também corrige o `ExceptionMiddleware`, que hoje devolve HTTP 200 em erros ([research.md](research.md) R6).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular (standalone components, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, SignalR (`ServiceOrderHub`), fila de WhatsApp (`IWhatsAppQueue`), Angular Material (dialog, table, button, progress spinner)

**Storage**: SQL Server via EF Core. Sem alteração de schema.

**Testing**: MSTest em `src/TVSApp.Tests` com fakes escritos à mão (padrão da feature 001)

**Target Platform**: API Linux (Docker) + SPA no navegador

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: N/A (uma atualização por clique)

**Constraints**: sem MediatR no fluxo novo; endpoints com `.ToResult()`; textos de UI em pt-BR; frontend e API publicados juntos

**Scale/Scope**: 2 rotas novas e 2 removidas; 2 métodos de handler; 1 propriedade e 2 métodos de domínio ajustados; 1 modal novo (4 arquivos); ajuste na Home; 1 linha no middleware

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Regra no Domain; orquestração (notificação, WhatsApp) no handler da Application; endpoint só faz composição (handler → hub → `ToResult()`). |
| II. Domain isolado | ✅ | `IsAwaitingEstimateResponse` e as guardas usam só tipos do Domain. |
| III. EF Core + migrations | ✅ | Nenhuma mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes de Domain (transições e guardas) e de Handler (404/409/sucesso/efeitos) em `TVSApp.Tests` (R8). |
| V. UI em português | ✅ | Título "Resposta", botões "RESPOSTA", "Aprovado", "Reprovado", "Cancelar"; mensagens em pt-BR. |
| Quality gates | ✅ | build + test + ng build no [quickstart.md](quickstart.md). |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/002-estimate-response-modal/
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
├── TVSApp.Domain/Entities/ServiceOrder.cs                  # IsAwaitingEstimateResponse + guardas
├── TVSApp.Application/
│   ├── Handlers/IServiceOrderCommandHandler.cs             # + ApproveEstimate, RejectEstimate
│   ├── Handlers/ServiceOrderCommandHandler.cs              # + INotificationCommandRepository
│   └── Requests/ServiceOrderRequests/ServiceOrderCommands/
│       ├── AddServiceOrderApproveEstimateCommand.cs        # REMOVER
│       └── AddServiceOrderRejectEstimateCommand.cs         # REMOVER
├── TVSApp.Api/
│   ├── Endpoints/ServiceOrderEndpoints.cs                  # 2 rotas REST novas; remove as antigas
│   └── Middlewares/ExceptionMiddleware.cs                  # StatusCode = code
└── TVSApp.Tests/
    ├── Domain/ServiceOrderEstimateResponseTests.cs         # NOVO
    └── Handlers/
        ├── Fakes/FakeNotificationCommandRepository.cs      # NOVO
        └── ServiceOrderCommandHandlerTests.cs              # + testes de approve/reject

web/src/app/features/home/
├── home.component.{ts,html}                                # coluna action, botão RESPOSTA, openEstimateResponseDialog
└── modals/estimate-response/                               # NOVO
    ├── estimate-response-modal.component.{ts,html,scss}
    └── estimate-response-modal.service.ts
```

**Structure Decision**: web application já existente. O modal novo fica ao lado do
`modals/estimate/` e segue o mesmo padrão de arquivos e nomes.

### Ordem de implementação sugerida

1. Domain (propriedade e guardas) + testes de domínio
2. Middleware (StatusCode)
3. Handler (approve/reject) + fake de notificação + testes
4. Endpoints REST e remoção das rotas e commands antigos
5. Angular: serviço e modal, depois a coluna e o botão na Home
6. Validação pelo quickstart

## Complexity Tracking

Nenhuma violação. As duas rotas REST divergem do padrão `verbo-recurso` das outras rotas, por
decisão explícita do usuário (Clarification Q2). Não é uma violação da constituição.
