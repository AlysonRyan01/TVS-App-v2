---

description: "Task list for 001-home-naming-consistency"
---

# Tasks: Padronização de nomenclatura das filas da tela Home

**Input**: Design documents from `/specs/001-home-naming-consistency/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos. A constituição (Princípio IV) exige testes em `TVSApp.Tests` para o
handler de orçamento, que é recriado (research.md R2/R6). O projeto de testes usa MSTest, com
fakes escritos à mão e sem biblioteca de mock.

**Organization**: as tarefas estão agrupadas por user story. US1 e US2 mexem nos mesmos arquivos,
então são feitas em sequência (US1 → US2), não em paralelo.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1, US2, US3 (ver spec.md)

## Path Conventions

- Backend: `src/TVSApp.{Domain,Application,Infrastructure,Api,Tests}/`
- Frontend: `web/src/app/features/home/`

## Regras gerais para todas as tarefas

- **Sem MediatR** no escopo: handlers comuns com interface, registrados com `AddScoped`.
- Endpoints no formato
  `async (IHandler handler, ...) => { var result = await handler.Metodo(...); return result.ToResult(); }`
  com `.WithTags("ServiceOrder").RequireAuthorization()` (extensão em
  `src/TVSApp.Api/Extensions/BaseResponseExtensions.cs`).
- Não alterar textos visíveis em português (rótulos de aba, títulos de coluna, mensagens).
- Não alterar enums de domínio (`ERepairStatus`, `ERepairResult`, `EServiceOrderStatus`), a
  entidade `ServiceOrder` nem `ServiceOrderDto` / `web/src/app/shared/models/service-order.interface.ts`.
- Nenhuma migration.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: confirmar o ponto de partida.

- [X] T001 Rodar `dotnet build TVSApp.sln` e `cd web && npx ng build` na raiz do repositório e anotar os warnings/erros já existentes, para não confundir com regressões (nenhum arquivo é alterado)
- [X] T002 [P] Criar a pasta de testes `src/TVSApp.Tests/Handlers/Fakes/` e confirmar que `src/TVSApp.Tests/TVSApp.Tests.csproj` referencia `TVSApp.Application` e `TVSApp.Domain` (já referencia; não adicionar pacotes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: substituir o envio de orçamento via MediatR, que hoje está quebrado (o
`AddServiceOrderEstimateHandler` foi removido no commit `0f7f7d3`), por um handler comum. Isso
bloqueia a US1 (modal) e a US3 (comportamento inalterado).

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase.

### Tests (escrever primeiro e ver falhar)

- [X] T003 [P] Criar fakes em `src/TVSApp.Tests/Handlers/Fakes/`: `FakeServiceOrderQueryRepository.cs` (implementa `IServiceOrderQueryRepository`, com `GetByIdForUpdateAsync` retornando uma `ServiceOrder` configurável ou 404 e os métodos de fila registrando qual foi chamado), `FakeServiceOrderCommandRepository.cs` (implementa `IServiceOrderCommandRepository` e guarda a última `ServiceOrder` passada a `UpdateAsync`) e `FakeWhatsAppQueue.cs` (implementa `IWhatsAppQueue` e guarda os itens enfileirados)
- [X] T004 Criar `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` (MSTest) cobrindo `AddEstimate`: (a) OS inexistente → `Code` 404 e mensagem "Essa ordem de serviço não existe"; (b) sucesso → `Code` 200, mensagem "Orçamento adicionado com sucesso!", `UpdateAsync` chamado; (c) `Warranty = " 90 dias "` → `serviceOrder.Guarantee` gravado como `"90 DIAS"`; (d) `PartPurchaseCost = 120` → `serviceOrder.PartPurchaseCost` = 120; (e) empresa ≠ `EEnterprise.PhilcoBritania` → 1 `SendEstimateMessageRequest` enfileirado; (f) `EEnterprise.PhilcoBritania` → nada enfileirado (depende de T003 e T005)

### Implementation

- [X] T005 Em `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddServiceOrderEstimateCommand.cs`: remover `: IRequest<BaseResponse<ServiceOrderDto?>>` e `using MediatR;`; renomear o parâmetro `Guarantee` → `Warranty` (no record, no construtor vazio e no `Normalize()`, mantendo `Warranty = Warranty.Trim().ToUpper()`); manter as regras do `Validate()` e as mensagens em português sem alteração
- [X] T006 Criar `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs` com `Task<BaseResponse<ServiceOrderDto?>> AddEstimate(AddServiceOrderEstimateCommand command);` (namespace `TVS_App.Application.Handlers`)
- [X] T007 Criar `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs` implementando `IServiceOrderCommandHandler`, com injeção por construtor de `IServiceOrderQueryRepository`, `IServiceOrderCommandRepository` e `IWhatsAppQueue`. O `AddEstimate` deve fazer, nesta ordem: `Normalize()` + `Validate()`; `GetByIdForUpdateAsync(ServiceOrderId)` (se `Data == null` → `new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe")`); `serviceOrder.AddEstimate(Solution, Warranty, TotalCost, RepairResult, EstimateMessage)`; `EditPartPurchaseCost(new PartPurchaseCost(PartPurchaseCost))`; `UpdateAsync`; se `Enterprise != EEnterprise.PhilcoBritania`, `Enqueue(new SendEstimateMessageRequest(Id.ToString(), SecurityCode, RepairResult!.Value, Customer.Name.CustomerName, EstimateMessage.ToUpper(), Customer.Phone.CustomerPhone))`; retornar `new BaseResponse<ServiceOrderDto?>(ServiceOrderMapper.MapToServiceOrderDto(serviceOrder), 200, "Orçamento adicionado com sucesso!")` (depende de T005, T006)
- [X] T008 Registrar `builder.Services.AddScoped<IServiceOrderCommandHandler, ServiceOrderCommandHandler>();` ao lado de `IServiceOrderQueryHandler` em `src/TVSApp.Api/Common/BuilderExtensions.cs` (manter `AddMediatR`, que ainda é usado por outros módulos)
- [X] T009 Reescrever o endpoint `PUT /add-service-order-estimate` em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs` para receber `(IServiceOrderCommandHandler handler, AddServiceOrderEstimateCommand command, IHubContext<ServiceOrderHub> hubContext)`, chamar `await handler.AddEstimate(command)`, enviar `hubContext.Clients.All.SendAsync("Atualizar", result.Message)` só se `result.IsSuccess`, e retornar `result.ToResult()`. Remover `ISender`, `Normalize()` e `Validate()` do endpoint (depende de T007, T008)
- [X] T010 Rodar `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests`. T004 deve passar

**Checkpoint**: o PUT de orçamento funciona sem MediatR. O frontend ainda envia `guarantee`,
então a US1 precisa ser concluída antes de publicar.

---

## Phase 3: User Story 1 - Vocabulário único por fila, de ponta a ponta (Priority: P1) 🎯 MVP

**Goal**: cada fila e o modal usam um único termo canônico em todas as camadas (glossário da
spec): `PendingEstimate`, `AwaitingResponse`, `Approved`, `Rejected`, `AwaitingPart`,
`ReadyForPickup`, `Estimate`.

**Independent Test**: a busca pelos termos antigos (quickstart.md §3, só nomes de tipo, método e
rota) não encontra nada, e as 6 abas e o modal funcionam.

### Tests for User Story 1

- [X] T011 [P] [US1] Criar `src/TVSApp.Tests/Handlers/ServiceOrderQueryHandlerTests.cs` (MSTest, usando `FakeServiceOrderQueryRepository` de T003): para cada um dos 6 métodos de fila do handler, verificar que o método de repositório correspondente foi chamado com o mesmo `pageNumber`/`pageSize` (`GetPendingEstimateServiceOrders` → `GetPendingEstimateAsync`, `GetAwaitingResponseServiceOrders` → `GetAwaitingResponseAsync`, `GetApprovedServiceOrders` → `GetApprovedAsync`, `GetRejectedServiceOrders` → `GetRejectedAsync`, `GetAwaitingPartServiceOrders` → `GetAwaitingPartAsync`, `GetReadyForPickupServiceOrders` → `GetReadyForPickupAsync`)

### Implementation for User Story 1 — backend

- [X] T012 [US1] Em `src/TVSApp.Domain/Repositories/Responses.cs`, renomear os records: `WaitingResponseServiceOrder` → `AwaitingResponseServiceOrder`, `RefusedServiceOrder` → `RejectedServiceOrder`, `WaitingPartServiceOrder` → `AwaitingPartServiceOrder`, `ReadyServiceOrder` → `ReadyForPickupServiceOrder` (`PendingEstimateServiceOrder` e `ApprovedServiceOrder` mantêm o nome; os campos ficam para a US2)
- [X] T013 [US1] Em `src/TVSApp.Domain/Repositories/ServiceOrders/IServiceOrderQueryRepository.cs`, renomear: `GetPendingEstimatesAsync` → `GetPendingEstimateAsync`, `GetWaitingResponseAsync` → `GetAwaitingResponseAsync`, `GetRefusedAsync` → `GetRejectedAsync`, `GetWaitingPartAsync` → `GetAwaitingPartAsync`, `GetReadyAsync` → `GetReadyForPickupAsync`, e atualizar os tipos de retorno para os records de T012 (depende de T012)
- [X] T014 [US1] Em `src/TVSApp.Infrastructure/Repositories/ServiceOrders/ServiceOrderQueryRepository.cs`, aplicar as renomeações de T012/T013 nas assinaturas, nos `new <Record>(...)` das projeções e nos `PaginatedResult<...>`/`BaseResponse<...>`. Renomear também as variáveis locais (ex.: `rejectedEstimateServiceOrders` → `rejectedServiceOrders`). Não alterar filtros (`Where`), ordenação nem mensagens (depende de T013)
- [X] T015 [US1] Em `src/TVSApp.Application/Handlers/IServiceOrderQueryHandler.cs` e `src/TVSApp.Application/Handlers/ServiceOrderQueryHandler.cs`, renomear: `GetWaitingResponseServiceOrders` → `GetAwaitingResponseServiceOrders`, `GetRefusedServiceOrders` → `GetRejectedServiceOrders`, `GetWaitingPartServiceOrders` → `GetAwaitingPartServiceOrders`, `GetReadyServiceOrders` → `GetReadyForPickupServiceOrders`, e apontar cada um para o método de repositório renomeado (depende de T013)
- [X] T016 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, renomear as 6 rotas conforme `contracts/http-endpoints.md` (`/get-pending-estimate-service-orders`, `/get-awaiting-response-service-orders`, `/get-approved-service-orders`, `/get-rejected-service-orders`, `/get-awaiting-part-service-orders`, `/get-ready-for-pickup-service-orders`, todas com `/{pageNumber}/{pageSize}`), chamar os métodos renomeados de T015 e manter `result.ToResult()` com `.WithTags("ServiceOrder").RequireAuthorization()`. Remover as rotas antigas, sem aliases (depende de T015)

### Implementation for User Story 1 — frontend

- [X] T017 [P] [US1] Em `web/src/app/features/home/models/`, renomear os arquivos e as interfaces: `waiting-response-service-orders.interface.ts` → `awaiting-response-service-order.interface.ts` (`AwaitingResponseServiceOrder`), `refused-service-orders.interface.ts` → `rejected-service-order.interface.ts` (`RejectedServiceOrder`), `waiting-part-service-orders.interface.ts` → `awaiting-part-service-order.interface.ts` (`AwaitingPartServiceOrder`), `ready-service-orders.interface.ts` → `ready-for-pickup-service-order.interface.ts` (`ReadyForPickupServiceOrder`), `pending-estimate-service-ordens.interface.ts` → `pending-estimate-service-order.interface.ts`, `approved-service-orders.interface.ts` → `approved-service-order.interface.ts` (usar `git mv`)
- [X] T018 [US1] Em `web/src/app/features/home/home.service.ts`, atualizar os imports de T017; renomear `getWaitingResponseServiceOrders` → `getAwaitingResponseServiceOrders`, `getRefusedServiceOrders` → `getRejectedServiceOrders`, `getWaitingPartServiceOrders` → `getAwaitingPartServiceOrders`, `getReadyServiceOrders` → `getReadyForPickupServiceOrders`; trocar as 6 URLs pelas rotas de T016; remover o import `signal`, que não é usado (depende de T017)
- [X] T019 [US1] Em `web/src/app/features/home/home.component.ts` e `home.component.html`, renomear o estado de cada fila para o termo canônico: `waitingResponse*` → `awaitingResponse*`, `refused*`/`isLoadingReprovedServiceOrders` → `rejected*`/`isLoadingRejectedServiceOrders`, `waitingPart*` → `awaitingPart*`, `ready*` → `readyForPickup*`, `pendingServiceOrdersPagination`/`pendingColumns`/`onPendingPageChange`/`loadPendingServiceOrders` → `pendingEstimate*` (sinais de lista, `isLoading*`, `*Pagination`, `*Columns`, `load*`, `on*PageChange`), e atualizar todas as referências no template (`[dataSource]`, `@if`, `[length]`, `[pageIndex]`, `(page)`) (depende de T018)
- [X] T020 [US1] Renomear a pasta `web/src/app/features/home/modals/quote/` → `modals/estimate/` (usar `git mv`) e os arquivos `add-service-order-quote-modal.component.{ts,html,scss}` → `add-service-order-estimate-modal.component.{ts,html,scss}`, `add-service-order-quote-modal.service.ts` → `add-service-order-estimate-modal.service.ts`, `models/add-service-order-quote.model.ts` → `models/add-service-order-estimate.model.ts`
- [X] T021 [US1] Nos arquivos de T020, renomear: `AddServiceOrderQuoteModalComponent` → `AddServiceOrderEstimateModalComponent` (selector `app-add-service-order-estimate-modal`, `styleUrl`/`templateUrl`), `AddServiceOrderQuoteModalService` → `AddServiceOrderEstimateModalService`, `addServiceOrderQuote` → `addServiceOrderEstimate`, interface `AddServiceOrderQuote` → `AddServiceOrderEstimate`, `sendQuote` → `sendEstimate` (no `.ts` e no `(ngSubmit)` do `.html`). Não alterar a tag HTML `<blockquote>` (depende de T020)
- [X] T022 [US1] Em `web/src/app/features/home/home.component.ts`, atualizar o import e o uso para `AddServiceOrderEstimateModalComponent` de `./modals/estimate/add-service-order-estimate-modal.component`; renomear `openDialog` → `openEstimateDialog` (e o `(click)` no `home.component.html`) (depende de T019, T021)
- [X] T023 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (T011 passa) e `cd web && npx ng build`

**Checkpoint**: o vocabulário está unificado. As 6 abas e o modal funcionam com as rotas novas.

---

## Phase 4: User Story 2 - Inglês correto e convencional nos identificadores (Priority: P2)

**Goal**: datas com padrão `<Evento>At`, campos ambíguos com nomes de domínio, `Warranty` no
contrato do orçamento e handlers com nomes descritivos.

**Independent Test**: a resposta JSON das 6 filas e o body do PUT têm exatamente os campos de
`data-model.md`, e `onTabChange` substitui `myTabSelectedTabChange`.

### Implementation for User Story 2 — backend

- [X] T024 [US2] Em `src/TVSApp.Domain/Repositories/Responses.cs`, renomear os parâmetros posicionais dos records (data-model.md): `PendingEstimateServiceOrder.EntryDate` → `EnteredAt` ("DateTime", não nulo); `AwaitingResponseServiceOrder.InspectionIn` → `EvaluatedAt` ("DateTime?"); `ApprovedServiceOrder.ApprovedIn` → `ApprovedAt` ("DateTime?"); `RejectedServiceOrder.RefusedIn` → `RejectedAt` ("DateTime?"); `AwaitingPartServiceOrder.PurchasedIn` → `PartPurchasedAt` ("DateTime?"); `ReadyForPickupServiceOrder.RepairDate` → `RepairedAt` ("DateTime?"), `Situation` → `RepairResult` ("string?"), `Response` → `RepairStatus` ("string?"). Manter a ordem dos parâmetros e a nulabilidade, para as projeções posicionais em `ServiceOrderQueryRepository.cs` continuarem válidas sem mudança
- [X] T025 [US2] Conferir em `src/TVSApp.Infrastructure/Repositories/ServiceOrders/ServiceOrderQueryRepository.cs` se alguma projeção usa argumentos nomeados ou propriedades dos records renomeados em T024 e ajustar se for o caso (depende de T024)
- [X] T026 [US2] Atualizar `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` e os fakes, caso referenciem nomes de campos alterados em T024 (depende de T024)

### Implementation for User Story 2 — frontend

- [X] T027 [P] [US2] Atualizar os campos das interfaces em `web/src/app/features/home/models/` para os nomes de T024 em camelCase: `enteredAt`, `evaluatedAt`, `approvedAt`, `rejectedAt`, `partPurchasedAt`, `repairedAt`, `repairResult` (`keyof typeof ERepairResult`), `repairStatus` (`keyof typeof ERepairStatus`)
- [X] T028 [US2] Em `web/src/app/features/home/home.component.ts` e `home.component.html`: trocar os `matColumnDef`, os arrays `*Columns` e os `element.<campo>` (`entryDate` → `enteredAt`, `inspectionIn` → `evaluatedAt`, `approvedIn` → `approvedAt`, `refusedIn` → `rejectedAt`, `purchasedIn` → `partPurchasedAt`, `repairDate` → `repairedAt`, `situation` → `repairResult`, `response` → `repairStatus`), mantendo os títulos de coluna em português; renomear `myTabSelectedTabChange` → `onTabChange` (incluindo o `(selectedTabChange)` no template); simplificar `createPaginationWithParams` para usar `data.pageSize`/`data.totalCount` em vez de `result.data!.…` (depende de T027)
- [X] T029 [US2] Em `web/src/app/features/home/modals/estimate/models/add-service-order-estimate.model.ts`, renomear `guarantee` → `warranty`. Em `add-service-order-estimate-modal.component.ts`/`.html`/`.scss`, renomear o form control `guarantee` → `warranty`, `guaranteePresets` → `warrantyPresets`, `setGuarantee` → `setWarranty` e a classe CSS `.guarantee` → `.warranty`. No `prefill`, mapear `order.guarantee` (do `ServiceOrder` compartilhado, que não muda) para o control `warranty`. Manter os rótulos visíveis em português
- [X] T030 [US2] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: o contrato está alinhado ao data-model.md.

---

## Phase 5: User Story 3 - Comportamento da tela inalterado (Priority: P1)

**Goal**: confirmar que nada mudou para quem usa a tela (FR-009, SC-004).

**Independent Test**: o roteiro de quickstart.md §4 com a mesma base de dados de antes.

- [ ] T031 [US3] Subir a API (`dotnet run --project src/TVSApp.Api`) e o Angular (`cd web && npx ng serve`) e executar quickstart.md §4: passar pelas 6 abas comparando registros, colunas, valores e ordenação; testar a paginação; enviar um orçamento com garantia pelo modal e conferir que a OS sai de "Orçamentos pendentes" e aparece na fila certa, com a garantia no detalhe e no PDF; simular falha da API e conferir a mensagem em português
- [ ] T032 [US3] Executar quickstart.md §5 no DevTools: o JSON das 6 filas tem exatamente os campos de `data-model.md`, e o PUT envia `warranty` (não `guarantee`)

**Checkpoint**: todas as stories estão validadas.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T033 [P] Executar quickstart.md §2 e §3 (sem `ISender`/`IRequest` no escopo; nenhum termo antigo)
- [X] T034 [P] Remover imports não usados nos arquivos alterados (ex.: `DialogRef`, `signal` em `home.component.ts`/`home.service.ts`) e rodar `cd web && npx ng build` de novo
- [X] T035 Remover o comentário "Sync Impact Report" do topo de `.specify/memory/constitution.md`, se ainda não tiver sido commitado, e marcar os itens concluídos neste `tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências
- **Foundational (Phase 2)**: depende do Setup e BLOQUEIA US1/US2/US3
- **US1 (Phase 3)**: depende da Foundational
- **US2 (Phase 4)**: depende da US1 (mesmos arquivos e nomes de tipo)
- **US3 (Phase 5)**: depende da US1 e da US2 (valida o resultado final)
- **Polish (Phase 6)**: depende de todas

### Within Each Story

- Backend: Domain → Infrastructure → Application → Api
- Frontend: models → service → component/template → modal
- Build e testes no fim de cada fase

### Parallel Opportunities

- T002 ∥ T001
- T003 ∥ T005/T006 (testes e command em arquivos diferentes)
- T011 ∥ T012
- T017 (models do frontend) ∥ T012–T016 (backend)
- T027 (models do frontend) ∥ T024–T026 (backend)
- T033 ∥ T034

---

## Parallel Example: User Story 1

```bash
# Backend e frontend da US1 em paralelo:
Task: "T012–T016 renomear records, repositório, handler e rotas no backend"
Task: "T017 renomear arquivos e interfaces em web/src/app/features/home/models/"
Task: "T011 criar ServiceOrderQueryHandlerTests.cs"
```

---

## Implementation Strategy

### MVP First

1. Phase 1 + Phase 2: o modal de orçamento volta a funcionar sem MediatR (corrige o defeito atual)
2. Phase 3 (US1): vocabulário unificado
3. **STOP and VALIDATE**: T023 + passada rápida pelas abas
4. Publicar frontend e API juntos

### Incremental Delivery

1. Foundational → corrige o orçamento
2. US1 → nomes unificados
3. US2 → campos e inglês corrigidos
4. US3 → validação final de comportamento
5. Cada fase termina com build e testes passando

---

## Notes

- Frontend e API MUST ser publicados juntos (rotas e campos JSON mudam).
- `[P]` = arquivos diferentes, sem dependência.
- Commitar ao fim de cada fase.
