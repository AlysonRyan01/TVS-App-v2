# Implementation Plan: Pagamentos da OS + comprovante

**Branch**: `015-payments` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/015-payments/spec.md`

## Summary

Criar a caixa **Pagamentos** (`app-payments-panel`) e colocá-la na etiqueta comum dos 6 modais da
Home e no painel da Buscar OS — os mesmos 7 lugares do sistema antigo — usando as rotas de
pagamento da feature 014 (agora com validação de valor e data). Cada pagamento ganha **Imprimir
comprovante**: novo PDF gerado no servidor (`GET /service-orders/{id}/payments/{paymentId}/receipt`)
com valor por extenso e resumo do saldo. Sem migration. Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22 (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, QuestPDF, Angular Material

**Storage**: SQL Server, sem mudança de schema

**Testing**: MSTest + fakes (validação, resumo, valor por extenso, handler do comprovante); frontend por build e roteiro manual

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: n/a

**Constraints**: mesmo comportamento do componente antigo; REST; pt-BR; SCSS de componente < 4 kB

**Scale/Scope**: 2 classes puras + 1 PDF + 1 método de handler + 1 rota; 1 componente Angular em 2 lugares

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Regras (validação, resumo, extenso) na Application; PDF na Infrastructure; endpoint só compõe. |
| II. Domain isolado | ✅ | Domain não muda. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | `PaymentReceiptSummaryTests`, `ValorPorExtensoTests`, validação dos commands e `GetPaymentReceipt`. |
| V. UI em português | ✅ | Textos e PDF em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/015-payments/
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
│   ├── Requests/ServiceOrderRequests/ServiceOrderCommands/{Add,Update}PaymentCommand.cs  # + Validate()
│   ├── Services/PaymentReceiptSummary.cs                    # NOVO
│   ├── Services/ValorPorExtenso.cs                          # NOVO
│   ├── Interfaces/Services/IGenerateServiceOrderPdf.cs      # + GeneratePaymentReceiptAsync
│   └── Handlers/IServiceOrderCommandHandler.cs, ServiceOrderCommandHandler.cs  # + GetPaymentReceipt; Validate nos de pagamento
├── TVSApp.Infrastructure/Services/
│   ├── GenerateServiceOrderPdfService.cs                    # + GeneratePaymentReceiptAsync
│   └── QuestPDFs/GeneratePaymentReceiptDocument.cs          # NOVO
├── TVSApp.Api/Endpoints/ServiceOrders/ServiceOrderQueryEndpoints.cs   # + GET .../payments/{paymentId}/receipt
└── TVSApp.Tests/
    ├── Application/PaymentReceiptSummaryTests.cs, ValorPorExtensoTests.cs   # NOVO
    └── Handlers/ServiceOrderCommandHandlerTests.cs (+ fake do PDF)

web/src/app/
├── shared/payments/payments-panel.component.{ts,html,scss}  # NOVO
├── shared/payments/payments.service.ts                      # NOVO
├── features/home/modals/shared/service-order-ticket.component.{ts,html}   # + <app-payments-panel>
└── features/search-service-orders/service-order-details-dialog/*          # + seção Pagamentos
```

**Structure Decision**: componente em `shared/payments/` porque é usado pela Home e pela Buscar OS.

### Ordem de implementação sugerida

1. Testes + `ValorPorExtenso` + `PaymentReceiptSummary`; validação dos commands.
2. PDF do comprovante + handler + rota (+ testes do handler).
3. Frontend: serviço, `app-payments-panel`, colocação na etiqueta e no painel.
4. Quickstart.

## Complexity Tracking

Nenhuma violação.
