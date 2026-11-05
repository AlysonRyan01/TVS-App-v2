---

description: "Task list for 003-part-purchased-modal"
---

# Tasks: Modal "Comprado" na aba "Aprovados"

**Input**: Design documents from `/specs/003-part-purchased-modal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos, porque são exigidos pelo FR-012 e pelo Princípio IV. MSTest com fakes
escritos à mão, que já existem em `src/TVSApp.Tests/Handlers/Fakes/`.

**Organization**: US1 entrega o fluxo completo (backend, modal e coluna). US2 reforça cancelar e
erros no modal.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Comportamento do backend idêntico a `old-handlers/AddPurchasedPartHandler.cs` (sem validações
  extras, sem notificação, sem WhatsApp).
- Sem MediatR; endpoint no formato
  `var result = await handler.X(id); if (result.IsSuccess) await hubContext.Clients.All.SendAsync("Atualizar", result.Message); return result.ToResult();`
  com `.WithTags("ServiceOrder").RequireAuthorization()`.
- Textos visíveis em português, exatamente como na spec.
- Sem migration.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (29 testes) e `cd web && npx ng build` na raiz e confirmar que tudo passa
- [X] T002 [P] Criar a pasta `web/src/app/features/home/modals/part-purchase/`

---

## Phase 2: Foundational

**Purpose**: não há base bloqueante nova. O handler, os fakes, o middleware e o padrão de modal
já existem desde as features 001 e 002.

- [X] T003 [P] Criar `src/TVSApp.Tests/Domain/ServiceOrderPartPurchaseTests.cs` (MSTest): `ServiceOrderFactory.Create()` + `AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg")` + `ApproveEstimate()`; depois `AddPurchasedPart()` → `ServiceOrderStatus == EServiceOrderStatus.OrderPart` e `PurchasePartDate` não nulo (cobre o método existente, que não tinha teste)

**Checkpoint**: `dotnet test` passa com o teste novo.

---

## Phase 3: User Story 1 - Registrar a compra da peça (Priority: P1) 🎯 MVP

**Goal**: o botão **COMPRADO** abre o modal; **Comprado** registra a compra e move a OS para
"Aguardando peça".

**Independent Test**: quickstart.md §3, passos 1, 2 e 4.

### Tests for User Story 1

- [X] T004 [P] [US1] Em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, adicionar: (a) `RegisterPartPurchase_ServiceOrderNotFound_Returns404` → `Code` 404, "Essa ordem de serviço não existe", `_commandRepository.LastUpdated` nulo; (b) `RegisterPartPurchase_ApprovedServiceOrder_RegistersPurchase` (OS de `AwaitingResponse()` + `ApproveEstimate()`) → `Code` 200, "Compra de peça adicionada com sucesso!", `Data` não nulo, `ServiceOrderStatus == OrderPart`, `PurchasePartDate` não nulo, `UpdateAsync` chamado com a mesma OS, `_notificationRepository.Created.Count == 0` e `_whatsAppQueue.Enqueued.Count == 0`

### Implementation for User Story 1

- [X] T005 [US1] Em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs`, adicionar `Task<BaseResponse<ServiceOrderDto?>> RegisterPartPurchase(long id);`. Em `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs`, implementar como o handler antigo: `GetByIdForUpdateAsync(id)`; se `Data == null` → `new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe")`; `serviceOrder.AddPurchasedPart()`; `await _commandRepository.UpdateAsync(serviceOrder)`; retornar `new BaseResponse<ServiceOrderDto?>(serviceOrder.MapToServiceOrderDto(), 200, "Compra de peça adicionada com sucesso!")`. Comentário apontando para `old-handlers/AddPurchasedPartHandler.cs`. Rodar T004, que deve passar
- [X] T006 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, remover o endpoint `PUT /add-service-order-purchased-part` inteiro (do `app.MapPut` até o `.RequireAuthorization();`) e adicionar `PUT /service-orders/{id:long}/part/purchase` conforme `contracts/http-endpoints.md`. Apagar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddPurchasedPartCommand.cs` com `git rm` (depende de T005)
- [X] T007 [P] [US1] Criar `web/src/app/features/home/modals/part-purchase/part-purchase-modal.service.ts` (`@Service()`), com `getServiceOrderById(id)` → `GET ${environment.apiUrl}/get-service-order-by-id/${id}` e `registerPartPurchase(id)` → `PUT ${environment.apiUrl}/service-orders/${id}/part/purchase` com body `{}`, ambos `Observable<BaseResponse<ServiceOrder>>`
- [X] T008 [US1] Criar `web/src/app/features/home/modals/part-purchase/part-purchase-modal.component.ts` (selector `app-part-purchase-modal`, classe `PartPurchaseModalComponent`) copiando a estrutura de `modals/estimate-response/estimate-response-modal.component.ts`: signals `isLoading`, `serviceOrder`, `isSending`; `loadServiceOrder()`; `protected registerPurchase()`, que trava (`isSending.set(true)` e `_dialogRef.disableClose = true`), chama `registerPartPurchase(this.id)` e, no `finalize`, destrava; em `next`, `_snackbar.open("Compra da peça registrada.")` e `_dialogRef.close(true)`; em `error`, `error.error?.message ?? (typeof error.error === 'string' ? error.error : 'Não foi possível registrar a compra. Tente de novo.')`. Imports: `CurrencyPipe`, `DatePipe`, `EnumLabelPipe`, `MatDialogModule`, `MatButtonModule`, `MatProgressSpinnerModule` (depende de T007)
- [X] T009 [US1] Criar `web/src/app/features/home/modals/part-purchase/part-purchase-modal.component.html` copiando `modals/estimate-response/estimate-response-modal.component.html`, com: título **Compra da peça**; aria-label do fechar "Fechar sem registrar"; mesmo `aside.ticket`; `section.estimate` com título "Orçamento aprovado" e os itens "Solução", "Valor total" (`totalAmount | currency: 'BRL' : 'symbol' : '1.2-2'`), "Custo da peça" (`partPurchaseCost | currency: 'BRL' : 'symbol' : '1.2-2'`), "Garantia" (ou "—"), "Aprovado em" (`responseDate | date: 'dd/MM/yyyy'` ou "—"); rodapé com o texto "A peça já foi comprada?", **Cancelar** (`mat-dialog-close`, `[disabled]="isSending()"`) e **Comprado** (`matButton="filled"`, `class="send"`, `(click)="registerPurchase()"`, `[disabled]="isSending()"`, spinner + "Registrando…" enquanto envia); mesmo estado de erro de carregamento (depende de T008)
- [X] T010 [P] [US1] Criar `web/src/app/features/home/modals/part-purchase/part-purchase-modal.component.scss` copiando `modals/estimate-response/estimate-response-modal.component.scss` e removendo o bloco `.reject` (não usado)
- [X] T011 [US1] Em `web/src/app/features/home/home.component.ts`: adicionar `'action'` ao final de `approvedColumns`; importar `PartPurchaseModalComponent`; criar `protected openPartPurchaseDialog(id: number)` com as mesmas opções de `openEstimateResponseDialog`, recarregando com `this.loadApprovedServiceOrders()` quando o modal fechar com `true`. Em `home.component.html`, na tabela de `approvedServiceOrders()`, depois do `matColumnDef="approvedAt"`, adicionar `<ng-container matColumnDef="action">` com `<th mat-header-cell *matHeaderCellDef class="cell-date">Ação</th>` e `<button matButton="filled" (click)="openPartPurchaseDialog(element.number)">COMPRADO</button>` (depende de T008)
- [X] T012 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: registrar a compra funciona de ponta a ponta.

---

## Phase 4: User Story 2 - Fechar sem registrar e tratar erros (Priority: P2)

**Goal**: cancelar não registra nada; erro mostra mensagem com o modal aberto; não há clique duplo.

**Independent Test**: quickstart.md §3, passos 3 e 5.

- [X] T013 [US2] Em `part-purchase-modal.component.html`/`.ts`, conferir e completar: o botão X do header com `[disabled]="isSending()"`; `registerPurchase()` ignorando a chamada se `isSending()` já for `true`; `disableClose` voltando a `false` no `finalize` (também em erro)
- [X] T014 [US2] Rodar `cd web && npx ng build`

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T015 [P] Executar quickstart.md §2 (a rota e o command antigos não existem mais; a rota nova tem `RequireAuthorization()`)
- [ ] T016 Executar quickstart.md §3 manualmente, com a API e o Angular rodando e usuário logado
- [X] T017 [P] Remover imports não usados nos arquivos alterados e rodar `dotnet build TVSApp.sln` e `cd web && npx ng build`

---

## Dependencies & Execution Order

- Setup → Foundational → US1 → US2 → Polish
- Dentro da US1: T004 → T005 → T006 (backend); T007 → T008 → T009 / T011 (frontend); T010 em paralelo
- US2 depende da US1 (mesmo arquivo do modal)

### Parallel Opportunities

- T002 ∥ T001
- T003 ∥ T004 ∥ T007 ∥ T010 (arquivos diferentes)
- Backend (T004–T006) ∥ frontend (T007–T011)
- T015 ∥ T017

## Parallel Example: User Story 1

```bash
Task: "T004 testes do handler em ServiceOrderCommandHandlerTests.cs"
Task: "T007 part-purchase-modal.service.ts"
Task: "T010 part-purchase-modal.component.scss"
```

## Implementation Strategy

1. Setup + Foundational
2. US1 → **STOP and VALIDATE** (quickstart §3) → MVP
3. US2 → robustez
4. Polish; publicar frontend e API juntos (a rota mudou)
