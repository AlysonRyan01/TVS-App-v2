# Implementation Plan: Tela "Vender"

**Branch**: `010-sell-page` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/010-sell-page/spec.md`

## Summary

Criar a tela `/vender` (mesmo layout de `/adicionar`) que registra a venda como **OS de venda**
(defeito "VENDA"), reproduzindo `old-handlers/CreateSalesServiceOrderHandler.cs` num método novo do
`ServiceOrderCommandHandler`, exposto em `POST /service-orders/sales` (REST, sem MediatR). A coluna
de cliente da Adicionar OS vira um componente comum. Sem migration. Decisões em
[research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22 (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, QuestPDF (`IGenerateServiceOrderPdf`), fila de WhatsApp, SignalR, Angular Material

**Storage**: SQL Server, **sem mudança de schema**

**Testing**: MSTest + fakes à mão; frontend por build e roteiro manual

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: n/a (uma requisição por venda)

**Constraints**: seguir o handler antigo; sem MediatR; REST; textos em pt-BR; pacote inicial < 1 MB

**Scale/Scope**: 1 método de handler + testes; 1 rota nova e 1 removida; 1 página nova; 1 componente extraído; 1 partial de estilos; 2 rotas sob demanda

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Fluxo na Application (handler); endpoint só compõe; PDF e fila já na Infrastructure. |
| II. Domain isolado | ✅ | Usa só métodos existentes de `ServiceOrder`; Domain não muda. |
| III. EF Core + migrations | ✅ | Nenhuma mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes de `CreateSalesServiceOrder` em `ServiceOrderCommandHandlerTests`. |
| V. UI em português | ✅ | Rótulos e mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/010-sell-page/
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
│   ├── Requests/ServiceOrderRequests/ServiceOrderCommands/CreateSalesServiceOrderCommand.cs  # sem IRequest
│   └── Handlers/IServiceOrderCommandHandler.cs, ServiceOrderCommandHandler.cs            # + CreateSalesServiceOrder
├── TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs          # POST /service-orders/sales; remove /create-sales-service-order
└── TVSApp.Tests/Handlers/
    ├── ServiceOrderCommandHandlerTests.cs                 # + testes de venda
    └── Fakes/FakeServiceOrderPdfGenerator.cs              # SaleResponse / SaleCalls

web/src/
├── styles.scss                                            # @use do partial
└── app/
    ├── app.routes.ts                                      # /adicionar e /vender com loadComponent
    ├── shared/layout/base-layout/base-layout.component.ts # "Vender" → /vender
    ├── shared/styles/_two-column-page.scss                # NOVO (estilos comuns das 2 telas)
    ├── features/customers/                                # MOVIDO de add-service-order/
    │   ├── customer-picker/customer-picker.component.{ts,html}   # NOVO (coluna ① Cliente)
    │   ├── customer-search/, customer-form/, customer-dialog/
    │   ├── customers.service.ts                            # busca, criar, atualizar, CEP
    │   └── models/{customer.model,address.model,document}.ts
    ├── features/add-service-order/                        # usa o picker; service só cria OS; sem SCSS próprio
    └── features/sell/                                     # NOVO
        ├── sell.component.{ts,html}
        ├── sell.service.ts
        └── models/create-sale.model.ts
```

**Structure Decision**: o que é de cliente vai para `features/customers/` e é usado pelas duas telas
pelo `app-customer-picker` (R4).

### Ordem de implementação sugerida

1. Testes de `CreateSalesServiceOrder` + fake do PDF; método no handler; command sem MediatR.
2. Rota `POST /service-orders/sales`; remover a antiga.
3. Frontend: extrair `features/customers/` + picker + partial (sem mudar `/adicionar`); rotas sob demanda.
4. Tela `/vender`, serviço, rota e menu.
5. Build, testes e roteiro do quickstart.

## Complexity Tracking

Nenhuma violação. Observações:
- A extração do picker mexe na tela da 009; o comportamento de `/adicionar` não muda.
- Rotas sob demanda são necessárias pelo orçamento de 1 MB (R5).
