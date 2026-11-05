---

description: "Task list for 007-rest-get-routes"
---

# Tasks: Rotas GET das tabelas no padrão REST

**Input**: Design documents from `/specs/007-rest-get-routes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: sem testes novos (research.md R3 / FR-007). Os `ServiceOrderQueryHandlerTests`
existentes continuam cobrindo a correspondência fila → consulta; as rotas HTTP são verificadas
pelo quickstart.

**Organization**: US1 troca as rotas (API + chamadas no frontend). US2 valida que a tela não mudou.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Endpoints: `.WithTags("ServiceOrder").RequireAuthorization()` e `return result.ToResult();`.
- Handlers, repositórios e respostas não mudam. Sem aliases para as rotas antigas.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (53 testes) e `cd web && npx ng build` e confirmar que tudo passa

---

## Phase 2: Foundational

Não há base bloqueante: as rotas novas usam os handlers existentes.

---

## Phase 3: User Story 1 - Leituras das tabelas no padrão REST (Priority: P1) 🎯 MVP

**Goal**: as 6 filas e a busca por número usam `/service-orders/...`; as rotas antigas não existem.

**Independent Test**: quickstart.md §2 e §3.

### Implementation for User Story 1 — API

- [X] T002 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, substituir os 6 `app.MapGet("/get-{fila}-service-orders/{pageNumber}/{pageSize}", ...)` por `app.MapGet("/service-orders/{fila}", async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) => { var result = await handler.<Método>(pageNumber, pageSize); return result.ToResult(); }).WithTags("ServiceOrder").RequireAuthorization();`, com `{fila}` e `<Método>` conforme `data-model.md`: `pending-estimate` → `GetPendingEstimateServiceOrders`, `awaiting-response` → `GetAwaitingResponseServiceOrders`, `approved` → `GetApprovedServiceOrders`, `rejected` → `GetRejectedServiceOrders`, `awaiting-part` → `GetAwaitingPartServiceOrders`, `ready-for-pickup` → `GetReadyForPickupServiceOrders`
- [X] T003 [US1] No mesmo arquivo, substituir `app.MapGet("/get-service-order-by-id/{id}", ...)` por `app.MapGet("/service-orders/{id:long}", async (IServiceOrderQueryHandler handler, long id) => { var result = await handler.GetServiceOrderById(id); return result.ToResult(); }).WithTags("ServiceOrder").RequireAuthorization();` (depende de T002, mesmo arquivo)
- [X] T004 [US1] Rodar `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests`

### Implementation for User Story 1 — Frontend

- [X] T005 [P] [US1] Em `web/src/app/features/home/home.service.ts`, trocar as 6 URLs `` `${environment.apiUrl}/get-{fila}-service-orders/${pagination.pageNumber}/${pagination.pageSize}` `` por `` `${environment.apiUrl}/service-orders/{fila}` `` com o segundo argumento `{ params: { pageNumber: pagination.pageNumber, pageSize: pagination.pageSize } }`, usando os mesmos 6 nomes de fila de T002. Não mudar nomes de métodos nem tipos
- [X] T006 [P] [US1] Nos 6 serviços de modal (`web/src/app/features/home/modals/estimate/add-service-order-estimate-modal.service.ts`, `estimate-response/estimate-response-modal.service.ts`, `part-purchase/part-purchase-modal.service.ts`, `rejected-release/rejected-release-modal.service.ts`, `repair/repair-modal.service.ts`, `delivery/delivery-modal.service.ts`), trocar em `getServiceOrderById` a URL `` `${environment.apiUrl}/get-service-order-by-id/${id}` `` por `` `${environment.apiUrl}/service-orders/${id}` ``
- [X] T007 [US1] Rodar `cd web && npx ng build` e quickstart.md §2 (grep sem resultados) (depende de T005, T006)

**Checkpoint**: nenhuma referência às rotas antigas; build e testes verdes.

---

## Phase 4: User Story 2 - Comportamento da tela inalterado (Priority: P1)

**Goal**: a tela mostra os mesmos dados, a mesma paginação e as mesmas mensagens.

**Independent Test**: quickstart.md §3 e §4.

- [ ] T008 [US2] Executar quickstart.md §3 (`curl`: padrão 1/25, página 2 com 5 itens, `pageNumber=0` → 400, `pageNumber=abc` → 400, sem token → 401, `/service-orders/1`), com a API rodando e um token válido
- [ ] T009 [US2] Executar quickstart.md §4 (6 abas, paginação e os 6 modais, conferindo as URLs no DevTools)

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T010 [P] Atualizar as referências às rotas antigas nos documentos de `specs/001-home-naming-consistency/` a `specs/006-delivery-modal/` (`contracts/http-endpoints.md` e `quickstart.md`), acrescentando no topo de cada contrato afetado a nota "Rotas GET substituídas pela feature 007 (`/service-orders/...`)", sem reescrever o histórico

---

## Dependencies & Execution Order

- Setup → US1 (API: T002 → T003 → T004; frontend: T005 ∥ T006 → T007) → US2 → Polish
- API e frontend da US1 podem ser feitos em paralelo; a validação (T007, US2) precisa dos dois.

### Parallel Opportunities

- T002–T004 (API) ∥ T005–T006 (frontend)
- T005 ∥ T006 (arquivos diferentes)
- T010 a qualquer momento depois da US1

## Parallel Example: User Story 1

```bash
Task: "T002–T003 endpoints REST em ServiceOrderEndpoints.cs"
Task: "T005 URLs + params no home.service.ts"
Task: "T006 getServiceOrderById nos 6 serviços de modal"
```

## Implementation Strategy

1. US1 (API + frontend juntos, porque as rotas antigas somem) → build e grep
2. US2 → validação manual
3. Publicar frontend e API juntos
