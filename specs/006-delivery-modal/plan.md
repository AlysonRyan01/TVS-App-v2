# Implementation Plan: Modal "Entregue" na aba "Prontos"

**Branch**: `006-delivery-modal` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/006-delivery-modal/spec.md`

## Summary

Adicionar à aba "Prontos" a coluna **Ação** com o botão **ENTREGUE**. O botão abre o modal
"Entrega" (mesmo design dos outros) com o botão **Entregue**, que registra a entrega. Para OS
consertada, gera o termo de entrega em PDF (aberto numa nova aba, ou baixado se a aba for
bloqueada) e envia o WhatsApp de garantia.

No backend, a rota quebrada `add-service-order-delivery` (MediatR sem handler) dá lugar a
`PUT /service-orders/{id}/delivery`. A lógica fica em `IServiceOrderCommandHandler.RegisterDelivery`,
reproduzindo exatamente `old-handlers/AddServiceOrderDeliveryAndReturnPdfHandler.cs`
([research.md](research.md) R1).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, SignalR, QuestPDF (via `IGenerateServiceOrderPdf`, já existe), fila de WhatsApp, Angular Material

**Storage**: SQL Server via EF Core. Sem alteração de schema.

**Testing**: MSTest + fakes escritos à mão; novo `FakeServiceOrderPdfGenerator`

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: N/A (a geração do PDF já existia)

**Constraints**: sem MediatR; UI em pt-BR; fluxo idêntico ao handler antigo; o PDF não pode ser bloqueado como pop-up (R3)

**Scale/Scope**: 1 rota nova e 1 removida; 1 método de handler + 1 dependência; 1 fake novo; 1 modal novo (4 arquivos); ajuste na Home; correção de 1 tipo no frontend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Handler na Application usa a abstração `IGenerateServiceOrderPdf` (implementada na Infrastructure); endpoint só faz composição (hub + arquivo/`ToResult()`). |
| II. Domain isolado | ✅ | Sem mudança no Domain. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Todas as saídas do fluxo testadas, mais o domínio de `AddDelivery()` (R6). |
| V. UI em português | ✅ | "ENTREGUE", "Entrega", "Entregue", "Cancelar", mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações. O endpoint devolve arquivo em vez de terminar sempre
em `ToResult()`; isso é necessário (FR-006) e está documentado em R2.

## Project Structure

### Documentation (this feature)

```text
specs/006-delivery-modal/
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
│   ├── Handlers/IServiceOrderCommandHandler.cs         # + Task<BaseResponse<byte[]>> RegisterDelivery(long id)
│   ├── Handlers/ServiceOrderCommandHandler.cs          # + IGenerateServiceOrderPdf no construtor; implementação
│   └── Requests/.../ServiceOrderCommands/AddServiceOrderDeliveryAndReturnPdfCommand.cs   # REMOVER
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs       # rota REST nova (PDF ou JSON); remove a antiga
└── TVSApp.Tests/
    ├── Domain/ServiceOrderDeliveryTests.cs             # NOVO
    └── Handlers/
        ├── Fakes/FakeServiceOrderPdfGenerator.cs       # NOVO
        └── ServiceOrderCommandHandlerTests.cs          # setup com o fake + testes de entrega

web/src/app/
├── shared/models/service-order.interface.ts            # repairStatus: ERepairStatus (R4)
└── features/home/
    ├── home.component.{ts,html}                        # coluna action + ENTREGUE + openDeliveryDialog
    └── modals/delivery/                                # NOVO
        ├── delivery-modal.component.{ts,html,scss}
        └── delivery-modal.service.ts
```

**Structure Decision**: o mesmo padrão das features 002 a 005. A diferença é o tratamento de
PDF no serviço e no componente do modal.

## Complexity Tracking

Nenhuma violação. Observações:
- A entrega fica salva mesmo se o PDF falhar (fidelidade ao antigo); isso está documentado num teste.
- São 6 modais repetindo o mesmo painel lateral e os mesmos estilos; a extração continua
  recomendada.
