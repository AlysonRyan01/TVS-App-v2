# Tasks: Limpeza das rotas da API (REST + comandos/consultas)

**Input**: Design documents from `/specs/014-rest-routes-cleanup/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos — FR-006 e a constituição (IV) exigem testes das regras novas (site e pagamentos).

**Organization**: US1 sistema interno, US2 site público, US3 API enxuta e organizada.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- Namespaces seguem o padrão existente (`TVS_App.*`; testes em `TVSApp.Tests.*`); endpoints novos em `TVS_App.Api.Endpoints.{Recurso}`

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (144 verdes), `cd web && npx ng build`; salvar a lista atual de rotas (`grep -rn "app.Map" src/TVSApp.Api`) para comparar no fim

---

## Phase 2: Foundational (estrutura de endpoints)

- [X] T002 Criar as pastas e arquivos vazios de endpoints em `src/TVSApp.Api/Endpoints/`: `ServiceOrders/ServiceOrderCommandEndpoints.cs` e `ServiceOrderQueryEndpoints.cs`, `Customers/CustomerCommandEndpoints.cs` e `CustomerQueryEndpoints.cs`, `Auth/AuthCommandEndpoints.cs`, `Metrics/MetricsQueryEndpoints.cs`, `Public/PublicCommandEndpoints.cs` e `PublicQueryEndpoints.cs` (cada um com `public static class` e `public static void Map{Nome}(this WebApplication app)`), e trocar as chamadas em `src/TVSApp.Api/Common/AppExtensions.cs` para os 8 métodos novos (os arquivos antigos saem nas fases seguintes)

---

## Phase 3: User Story 1 — Sistema interno continua funcionando (P1) 🎯 MVP

**Goal**: todas as rotas usadas pelo Angular nos arquivos novos, com estimativa e auth renomeadas.

**Independent Test**: quickstart §3.

- [X] T003 [US1] Mover para `ServiceOrders/ServiceOrderQueryEndpoints.cs` (sem mudar comportamento): as 6 filas da Home, `GET /service-orders/search`, `GET /service-orders/{id}`, `GET /service-orders/{id}/pdf`
- [X] T004 [US1] Mover para `ServiceOrders/ServiceOrderCommandEndpoints.cs`: `POST /service-orders`, `POST /service-orders/sales`, `PUT /service-orders/{id}`, `/estimate/approve`, `/estimate/reject`, `/ready-for-pickup`, `/part/purchase`, `/part/purchase-cost` (com o record `UpdatePartPurchaseCostRequest`), `/repair`, `/delivery`, `/product/location`; e trocar `PUT /add-service-order-estimate` por `PUT /service-orders/{id:long}/estimate` (o id vem da rota: `command with { ServiceOrderId = id }`), mantendo SignalR e respostas
- [X] T005 [P] [US1] Mover para `Customers/CustomerQueryEndpoints.cs` (`GET /customers`, `GET /addresses/{cep}`) e `Customers/CustomerCommandEndpoints.cs` (`POST /customers`, `PUT /customers/{id}`)
- [X] T006 [P] [US1] Mover para `Metrics/MetricsQueryEndpoints.cs` o `GET /metrics`
- [X] T007 [P] [US1] Criar `Auth/AuthCommandEndpoints.cs` com o conteúdo de `AuthEndpoints.cs` nos caminhos `POST /auth/login`, `/auth/logout`, `/auth/refresh` e `/auth/register` (este com `.RequireAuthorization()`); mesmo comportamento e respostas
- [X] T008 [US1] No Angular: `web/src/app/features/home/modals/estimate/add-service-order-estimate-modal.service.ts` → `PUT ${apiUrl}/service-orders/${id}/estimate`; `web/src/app/core/auth/auth.service.ts` e `auth-interceptor.ts` → `/auth/login`, `/auth/logout`, `/auth/refresh` (inclusive a lista de rotas que o interceptor não intercepta)

**Checkpoint**: sistema interno funcionando com as rotas novas.

---

## Phase 4: User Story 2 — Site público continua funcionando (P1)

**Goal**: as 4 rotas do site com os mesmos caminhos, sem login, com conferência do código.

**Independent Test**: quickstart §4 (site sem alterações).

- [X] T009 [US2] Testes em `src/TVSApp.Tests/Handlers/ServiceOrderQueryHandlerTests.cs` para `GetForCustomer(id, code)`: OS inexistente → 404 "Essa ordem de serviço não existe"; código errado → mesmo 404; código com espaços/minúsculas é normalizado; ok → 200 com `EstimateServiceOrder` (número, cliente, tipo, marca, modelo, defeito, garantia, total, mensagem, situação, resultado, status do orçamento, empresa) — ajustar `FakeServiceOrderQueryRepository` para devolver uma OS em `GetByIdAsync` quando configurada
- [X] T010 [US2] Implementar `Task<BaseResponse<EstimateServiceOrder>> GetForCustomer(long id, string code)` em `src/TVSApp.Application/Handlers/IServiceOrderQueryHandler.cs`/`ServiceOrderQueryHandler.cs` seguindo `old-handlers/GetServiceOrderForCustomerHandler.cs` (com a mensagem única do research R4); apagar `GetServiceOrdersForCustomerQuery.cs`; T009 deve passar
- [X] T011 [US2] Testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` para `ApproveEstimateByCustomer(id, code)` e `RejectEstimateByCustomer(id, code)`: código errado ou OS inexistente → 404 "Essa ordem de serviço não existe" sem gravar nem WhatsApp; código certo → mesmo resultado de `ApproveEstimate`/`RejectEstimate` (status, WhatsApp, aviso ao dono, notificação); orçamento já respondido → 409 igual ao da Home
- [X] T012 [US2] Implementar `ApproveEstimateByCustomer` e `RejectEstimateByCustomer` em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs`/`ServiceOrderCommandHandler.cs` (confere o código com trim/maiúsculas contra `SecurityCode` via `GetByIdForUpdateAsync` e delega para `ApproveEstimate(id)`/`RejectEstimate(id)`); criar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/CustomerEstimateResponseRequest.cs` (`record CustomerEstimateResponseRequest(long Id, string Code)`); T011 deve passar
- [X] T013 [US2] Preencher `Public/PublicQueryEndpoints.cs` (`GET /get-service-order-for-customer/{id}/{code}` → `GetForCustomer` → `ToResult()`) e `Public/PublicCommandEndpoints.cs` (`PUT /add-service-order-approve-estimate` e `PUT /add-service-order-reject-estimate` com corpo `CustomerEstimateResponseRequest` → SignalR "Atualizar" no sucesso → `ToResult()`; e o `POST /v1/whatsapp/site-contact` movido de `WhatsAppEndpoints.cs` sem mudança); nenhuma com `RequireAuthorization`

**Checkpoint**: site público funcionando com a API nova, sem alterar o `js/api.js`.

---

## Phase 5: User Story 3 — API enxuta e organizada (P2)

**Goal**: pagamentos em REST, rotas sem uso removidas, sem MediatR.

**Independent Test**: quickstart §2.

### Pagamentos (mantidos, em REST)

- [X] T014 [US3] Ler `old-handlers/{Get,Add,Update,Remove}PaymentHandler.cs` e escrever testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`/`ServiceOrderQueryHandlerTests.cs` para `GetPayments(id)`, `AddPayment(id, command)`, `UpdatePayment(id, paymentId, command)`, `RemovePayment(id, paymentId)` com as mesmas validações e mensagens dos antigos (OS inexistente, pagamento inexistente, valores)
- [X] T015 [US3] Implementar os 4 métodos (consulta no `ServiceOrderQueryHandler`, comandos no `ServiceOrderCommandHandler`) usando `ServiceOrder.AddPayment/UpdatePayment/RemovePayment`; reescrever `AddPaymentCommand`, `UpdatePaymentCommand` sem MediatR (sem os ids, que vêm da rota) e apagar `RemovePaymentCommand.cs` e `GetPaymentsQuery.cs`; rotas `GET /service-orders/{id}/payments` (query) e `POST /service-orders/{id}/payments`, `PUT /service-orders/{id}/payments/{paymentId}`, `DELETE /service-orders/{id}/payments/{paymentId}` (command), todas com `RequireAuthorization`; T014 deve passar

### Remoção

- [X] T016 [US3] Apagar `src/TVSApp.Api/Endpoints/{ServiceOrder,Customer,Auth,Dashboard,Notification,Email,WhatsApp}Endpoints.cs` (as rotas usadas já estão nos arquivos novos; as 9 sem uso somem junto)
- [X] T017 [US3] Apagar o código que só as rotas removidas usavam (research R2): `GetAllCustomersHandler`, `GetCustomerByIdHandler`, `GetAllCustomersQuery`, `GetCustomerByIdQuery`, `ICustomerQueryRepository.GetAllAsync` (+ implementação e fake); `GetDeliveredQuery`; `GetServiceOrdersByEnterpriseQuery`, `IServiceOrderQueryRepository.GetByEnterpriseAsync` (+ implementação e fake); `GetAbandonedOrdersHandler`, `GetAbandonedOrdersQuery`, `IMetricsService.GetAbandonedOrdersAsync` (+ `MetricsService`, `FakeMetricsService`), `AbandonedOrdersDto`/`AbandonedOrderDto`; handlers e requests MediatR de notificação (`src/TVSApp.Application/Handlers/Notifications/`, `Requests/NotificationRequests/`); `UpdateServiceOrderCommand`, `UpdateServiceOrderResponseDto`, `GetServiceOrderByIdQuery`
- [X] T018 [US3] Remover o envio de e-mail: `IEmailQueue`, `EmailQueue`, `EmailNotificationWorker`, `IEmailService`, `EmailService`, `Requests/EmailQueueRequests/`, a configuração `EmailServiceSettings` e seus registros em `src/TVSApp.Api/Common/BuilderExtensions.cs` (e a seção do `appsettings*.json`, se houver)
- [X] T019 [US3] Remover MediatR: `AddMediatR` em `BuilderExtensions.cs` e o `PackageReference` de MediatR nos `.csproj`; `grep -rn "MediatR\|ISender\|IRequest" src` deve vir vazio

---

## Phase 6: Polish & validação

- [X] T020 [P] `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` verdes; listar as rotas (`grep -rn "app.Map" src/TVSApp.Api`) e conferir as 37 do data-model.md
- [X] T021 [P] `cd web && npx ng build` sem erro; `grep -rn "add-service-order-estimate\|/refresh-token\|apiUrl}/login\|apiUrl}/logout" web/src` vazio
- [ ] T022 Roteiro manual do quickstart §3 (sistema interno) e §4 (site público sem alterações)

---

## Dependencies & Execution Order

- T001 → T002 → US1, US2, US3.
- US1: T003 → T004 (mesmo arquivo de OS em sequência); T005 ∥ T006 ∥ T007; T008 depois de T004 e T007.
- US2: T009 → T010; T011 → T012; T013 depois de T010 e T012.
- US3: T014 → T015 (depois de T003/T004, que criam os arquivos de OS); T016 depois de US1, US2 e T015; T017 → T018 → T019.
- Polish por último.

### Parallel Opportunities

- T005 ∥ T006 ∥ T007; T009 ∥ T011 (arquivos de teste diferentes); T020 ∥ T021.

### Parallel Example

```text
T005 clientes  |  T006 métricas  |  T007 auth  |  T009 testes da consulta do cliente
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 + US2 — as duas pontas (Angular e site) funcionando com a
   nova organização, antes de apagar qualquer coisa.
2. **US3**: pagamentos em REST e remoção do código morto, com build/testes a cada passo.
3. Publicar a API junto com o Angular (o site não muda).
