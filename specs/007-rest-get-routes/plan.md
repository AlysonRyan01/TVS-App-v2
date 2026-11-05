# Implementation Plan: Rotas GET das tabelas no padrão REST

**Branch**: `007-rest-get-routes` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/007-rest-get-routes/spec.md`

## Summary

Trocar as 7 rotas de leitura usadas pela Home (6 filas e a busca por número) pelo padrão REST:
`GET /service-orders/{fila}?pageNumber=&pageSize=` (padrão 1 e 25) e `GET /service-orders/{id}`.
Handlers, repositórios e respostas não mudam; mudam só os endpoints, o `HomeService` e os 6
serviços de modal ([research.md](research.md)).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular

**Primary Dependencies**: ASP.NET Core Minimal API (binding de query com valor padrão), Angular `HttpClient` (`params`)

**Storage**: N/A (sem mudança de dados)

**Testing**: MSTest (os testes de handler existentes continuam valendo); rotas verificadas pelo quickstart (R3)

**Target Platform**: API Linux (Docker) + SPA

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: N/A

**Constraints**: respostas idênticas; frontend e API publicados juntos; sem aliases

**Scale/Scope**: 7 endpoints trocados em 1 arquivo; 1 serviço da Home; 6 serviços de modal

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Só a camada Api (endpoints) e o frontend mudam. |
| II. Domain isolado | ✅ | Sem mudança no Domain. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Sem regra nova; o mapeamento fila → consulta já está coberto por `ServiceOrderQueryHandlerTests`. Os testes de rota HTTP ficam como recomendação (R3). |
| V. UI em português | ✅ | Nenhum texto muda. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/007-rest-get-routes/
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
src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs                # 7 MapGet trocados

web/src/app/features/home/
├── home.service.ts                                              # 6 URLs + params
└── modals/
    ├── estimate/add-service-order-estimate-modal.service.ts     # getServiceOrderById
    ├── estimate-response/estimate-response-modal.service.ts     # getServiceOrderById
    ├── part-purchase/part-purchase-modal.service.ts             # getServiceOrderById
    ├── rejected-release/rejected-release-modal.service.ts       # getServiceOrderById
    ├── repair/repair-modal.service.ts                           # getServiceOrderById
    └── delivery/delivery-modal.service.ts                       # getServiceOrderById
```

**Structure Decision**: sem arquivos novos de código; só alterações.

## Complexity Tracking

Nenhuma violação. Observações:
- O `getServiceOrderById` está repetido em 6 serviços de modal. Isso entra na mesma refatoração
  recomendada para o painel lateral compartilhado.
- Um projeto de testes de integração da Api (R3) cobriria as rotas de todas as features (001 a 007).
