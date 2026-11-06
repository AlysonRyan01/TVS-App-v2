# Implementation Plan: Tela "Adicionar OS" (cliente + nova ordem de serviço)

**Branch**: `009-add-service-order-page` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/009-add-service-order-page/spec.md`

## Summary

Criar a tela `/adicionar` (duas colunas: cliente | aparelho, com "Criar OS" fixo no rodapé). Nela,
a pessoa busca o cliente pelo nome, confirma ou atualiza os dados num modal central (ou cadastra
um cliente novo) e cria a OS, que gera o termo de entrada em PDF (impresso automaticamente) e o
WhatsApp de boas-vindas.

No backend:
- o documento do cliente deixa de ser só CPF: o campo `Cpf` vira **`Document`** (CPF ou CNPJ
  alfanumérico, opcional), com uma migration que renomeia a coluna e a torna anulável;
- as rotas de cliente, de CEP e de criação de OS passam para REST, com handlers sem MediatR;
- a criação de OS reproduz `old-handlers/CreateServiceOrderAndReturnPdfHandler.cs` ([research.md](research.md)).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22 (standalone, signals)

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core (SQL Server), QuestPDF (via `IGenerateServiceOrderPdf`), ViaCEP (via `IAddressService`), fila de WhatsApp, SignalR, Angular Material (form-field, select, dialog, button)

**Storage**: SQL Server; **1 migration** (`RenameCustomerCpfToDocument`: rename + nullable)

**Testing**: MSTest + fakes escritos à mão (Domain e Application); frontend validado por build e roteiro manual

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: busca com debounce de 300 ms, até 21 resultados

**Constraints**: sem MediatR nos fluxos novos; REST; textos em pt-BR; migration sem perda de dados; frontend e API publicados juntos

**Scale/Scope**: 1 value object + 1 enum novos; 1 migration; 3 handlers de cliente reescritos; 1 método novo no command handler de OS; 5 rotas trocadas; 1 página nova com 2 componentes, 1 modal e 1 serviço; 1 utilitário de PDF extraído

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | `Document` no Domain; handlers na Application; repositórios, migration e ViaCEP na Infrastructure; endpoints só fazem composição. |
| II. Domain isolado | ✅ | `Document` e `EDocumentType` só usam tipos do Domain (validação com `Regex` do BCL). |
| III. EF Core + migrations | ✅ | Mudança de schema só por migration, revisada à mão para ser rename (sem perda de dados), com `Down` (R2). |
| IV. Regras de negócio testadas | ✅ | `DocumentTests`, `CustomerHandlerTests` e testes de `CreateServiceOrder` (R8). |
| V. UI em português | ✅ | Todos os rótulos e mensagens em pt-BR; "CPF/CNPJ" na tela. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/009-add-service-order-page/
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
├── TVSApp.Domain/
│   ├── Enums/EDocumentType.cs                                   # NOVO
│   ├── ValueObjects/Customer/Document.cs                        # NOVO (substitui Cpf.cs, que é removido)
│   ├── Entities/Customer.cs                                     # Document? + UpdateDocument
│   └── Repositories/Customers/ICustomerQueryRepository.cs       # GetCustomerByName(name, limit)
├── TVSApp.Application/
│   ├── DTOs/CustomerDto.cs, DTOs/ServiceOrder/ServiceOrderDto.cs, DTOs/AddressDto.cs
│   ├── Mappers/CustomerMapper.cs, Mappers/ServiceOrderMapper.cs
│   ├── Requests/CustomerRequests/CustomerCommands/{Create,Update}CustomerCommand.cs   # Document, sem IRequest
│   ├── Requests/ServiceOrderRequests/ServiceOrderCommands/CreateServiceOrderCommand.cs # sem IRequest
│   ├── Requests/ServiceOrderRequests/ServiceOrderCommands/EditServiceOrderCommand.cs   # Document opcional
│   └── Handlers/
│       ├── Customers/ICustomerQueryHandler.cs, CustomerQueryHandler.cs        # NOVO
│       ├── Customers/ICustomerCommandHandler.cs, CustomerCommandHandler.cs    # NOVO
│       ├── Customers/{CreateCustomer,UpdateCustomer,GetCustomerByName}Handler.cs  # REMOVER (MediatR)
│       └── IServiceOrderCommandHandler.cs / ServiceOrderCommandHandler.cs     # + CreateServiceOrder
├── TVSApp.Infrastructure/
│   ├── Data/Mappings/CustomerMapping.cs                         # Document
│   ├── Migrations/<ts>_RenameCustomerCpfToDocument.cs           # NOVO
│   ├── Repositories/Customers/CustomerQueryRepository.cs        # limit + ordem
│   └── Services/AddressService/AddressService.cs                # UF + "erro" → 404
├── TVSApp.Api/
│   ├── Endpoints/CustomerEndpoints.cs                           # /customers, /addresses
│   ├── Endpoints/ServiceOrderEndpoints.cs                       # POST /service-orders
│   └── Common/BuilderExtensions.cs                              # DI dos handlers; CORS expõe X-Service-Order-Id
└── TVSApp.Tests/
    ├── Domain/DocumentTests.cs                                  # NOVO
    ├── Handlers/CustomerHandlerTests.cs                         # NOVO
    ├── Handlers/ServiceOrderCommandHandlerTests.cs              # + CreateServiceOrder
    └── Handlers/Fakes/{FakeCustomerQueryRepository,FakeCustomerCommandRepository}.cs  # NOVO; ajustes nos fakes de OS e PDF

web/src/app/
├── app.routes.ts                                                # /adicionar
├── shared/layout/base-layout/base-layout.component.ts           # "Adicionar OS" → /adicionar
├── shared/models/service-order.interface.ts                     # cpf → document
├── shared/pdf/pdf-print.ts                                      # NOVO (extraído da entrega)
├── features/home/modals/delivery/delivery-modal.component.ts    # usa pdf-print
└── features/add-service-order/                                  # NOVO
    ├── add-service-order.component.{ts,html,scss}
    ├── add-service-order.service.ts
    ├── customer-search/customer-search.component.{ts,html,scss}
    ├── customer-dialog/customer-dialog.component.{ts,html,scss}
    └── models/{customer,create-customer,create-service-order,address}.model.ts
```

**Structure Decision**: a página fica em `features/add-service-order/`, irmã de `features/home/`,
com os subcomponentes dentro da própria pasta.

### Ordem de implementação sugerida

1. Domain: `Document` + testes; `Customer`.
2. Infrastructure: mapping + migration (revisar à mão) + repositório + ViaCEP.
3. Application: rename `Cpf` → `Document`; handlers de cliente; `CreateServiceOrder` + testes.
4. Api: rotas REST, DI e CORS.
5. Frontend: `pdf-print` extraído; serviço; busca; modal; página; rota e menu.
6. Validação pelo quickstart.

## Complexity Tracking

Nenhuma violação. Observações:
- A renomeação `Cpf` → `Document` atravessa várias camadas e é pré-requisito do resto (ordem 1–3).
- `AddressDto` muda de nomes em português para inglês (consistência da feature 001) junto com a
  troca da rota; nenhuma tela usava a rota antiga.
