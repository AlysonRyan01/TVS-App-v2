---

description: "Task list for 004-rejected-release-modal"
---

# Tasks: Modal "Liberado" na aba "Reprovados"

**Input**: Design documents from `/specs/004-rejected-release-modal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos, porque são exigidos pelo FR-010 e pelo Princípio IV. MSTest com os fakes de
`src/TVSApp.Tests/Handlers/Fakes/`.

**Organization**: a Fase 2 ajusta os fakes; US1 entrega o fluxo completo; US2 reforça cancelar e
erros no modal.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Backend idêntico a `old-handlers/MarkEstimateRejectedAsReadyToPickUpHandler.cs` (ordem das
  checagens, mensagens, WhatsApp para todas as empresas, sem notificação interna).
- Sem MediatR; endpoint no formato
  `var result = await handler.X(id); if (result.IsSuccess) await hubContext.Clients.All.SendAsync("Atualizar", result.Message); return result.ToResult();`
  com `.WithTags("ServiceOrder").RequireAuthorization()`.
- Textos visíveis em português, exatamente como na spec. Sem migration.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (32 testes) e `cd web && npx ng build` e confirmar que tudo passa
- [X] T002 [P] Criar a pasta `web/src/app/features/home/modals/rejected-release/`

---

## Phase 2: Foundational (fakes)

- [X] T003 [P] Em `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderCommandRepository.cs`, adicionar `public BaseResponse<ServiceOrder?>? UpdateResponse { get; set; }`; o `UpdateAsync` grava `LastUpdated` e retorna `UpdateResponse ?? new BaseResponse<ServiceOrder?>(serviceOrder)` (padrão = sucesso; os testes atuais continuam passando)
- [X] T004 [P] Em `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderQueryRepository.cs`, adicionar `public bool GetByIdForUpdateCalled { get; private set; }` e defini-lo como `true` dentro de `GetByIdForUpdateAsync`
- [X] T005 [P] Criar `src/TVSApp.Tests/Domain/ServiceOrderRejectedReleaseTests.cs` (MSTest): (a) OS de `ServiceOrderFactory.Create()` + `AddEstimate(..., ERepairResult.Repair, ...)` + `RejectEstimate()`, depois `MarkEstimateRejectedAsReadyToPickUp()` → `ServiceOrderStatus == EServiceOrderStatus.ReadyToPickUp` e `RepairStatus == ERepairStatus.Disapproved`; (b) OS aguardando resposta (sem reprovar) → `Assert.ThrowsException<EntityException<ServiceOrder>>` com mensagem "O orçamento precisa estar rejeitado para marcar como pronto para entrega"

**Checkpoint**: `dotnet test` passa (34 testes).

---

## Phase 3: User Story 1 - Liberar OS reprovada para retirada (Priority: P1) 🎯 MVP

**Goal**: **LIBERADO** abre o modal; **Liberado** marca a OS como pronta para entrega, envia o
WhatsApp e move a OS para "Prontos".

**Independent Test**: quickstart.md §3, passos 1, 2 e 4.

### Tests for User Story 1

- [X] T006 [US1] Em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, adicionar (helper `Rejected(EEnterprise)` = `AwaitingResponse(enterprise)` + `RejectEstimate()`): (a) `MarkRejected_IdZero_Returns400WithoutQuerying` → `Code` 400, "O número da ordem de serviço não pode ser 0", `_queryRepository.GetByIdForUpdateCalled == false`; (b) `MarkRejected_NotFound_Returns404WithRepositoryMessage` → `Code` 404, `Message == "A ordem de serviço com ID:5 não existe"` (id 5); (c) `MarkRejected_NotRejected_Throws` (OS de `AwaitingResponse()`) → `Assert.ThrowsExceptionAsync<EntityException<ServiceOrder>>`, `LastUpdated` nulo e 0 WhatsApp; (d) `MarkRejected_UpdateFails_ReturnsUpdateErrorWithoutWhatsApp` (`_commandRepository.UpdateResponse = new(null, 500, "Erro ao salvar")`) → `Code` 500, `Message` "Erro ao salvar", 0 WhatsApp; (e) `[DataRow(Particular)] [DataRow(PhilcoBritania)] MarkRejected_Rejected_MarksReadyAndSendsWhatsApp` → `Code` 200, "Ordem de serviço marcada como pronta para entrega", `ServiceOrderStatus == ReadyToPickUp`, `RepairStatus == Disapproved`, 1 `SendRejectionReadyForPickupMessageRequest` com `CustomerName == "JOÃO DA SILVA"` e `PhoneNumber == "41999999999"`, `_notificationRepository.Created.Count == 0` (depende de T003, T004)

### Implementation for User Story 1

- [X] T007 [US1] Em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs`, adicionar `Task<BaseResponse<ServiceOrderDto?>> MarkRejectedEstimateAsReadyForPickup(long id);`. Em `ServiceOrderCommandHandler.cs`, implementar na ordem de research.md R1: `id == 0` → `(null, 400, "O número da ordem de serviço não pode ser 0")`; `GetByIdForUpdateAsync(id)` com `!IsSuccess || Data == null` → `(null, 404, result.Message)`; `serviceOrder.MarkEstimateRejectedAsReadyToPickUp()` (sem try/catch); `var update = await _commandRepository.UpdateAsync(serviceOrder)`; se `!update.IsSuccess` → `(null, update.Code, update.Message)`; `_whatsAppQueue.Enqueue(new SendRejectionReadyForPickupMessageRequest(serviceOrder.Customer.Name.CustomerName, serviceOrder.Customer.Phone.CustomerPhone))`; retornar `(serviceOrder.MapToServiceOrderDto(), 200, "Ordem de serviço marcada como pronta para entrega")`. Comentário apontando para o handler antigo. Rodar T006, que deve passar
- [X] T008 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, remover o endpoint `PUT /mark-estimate-rejected-as-ready-to-pick-up` inteiro (do `app.MapPut` até o `.RequireAuthorization();`) e adicionar `PUT /service-orders/{id:long}/ready-for-pickup` conforme `contracts/http-endpoints.md`. Apagar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/MarkEstimateRejectedAsReadyToPickUpCommand.cs` com `git rm` (depende de T007)
- [X] T009 [P] [US1] Criar `web/src/app/features/home/modals/rejected-release/rejected-release-modal.service.ts` (`@Service()`, classe `RejectedReleaseModalService`), com `getServiceOrderById(id)` → `GET ${environment.apiUrl}/get-service-order-by-id/${id}` e `markAsReadyForPickup(id)` → `PUT ${environment.apiUrl}/service-orders/${id}/ready-for-pickup` com body `{}`
- [X] T010 [US1] Criar `rejected-release-modal.component.ts` (selector `app-rejected-release-modal`, classe `RejectedReleaseModalComponent`) copiando `modals/part-purchase/part-purchase-modal.component.ts`, trocando o serviço, renomeando `registerPurchase()` → `release()`, chamando `markAsReadyForPickup(this.id)`, com a mensagem de sucesso "Ordem de serviço liberada para retirada." e o fallback de erro "Não foi possível liberar a ordem de serviço. Tente de novo." (depende de T009)
- [X] T011 [US1] Criar `rejected-release-modal.component.html` copiando `modals/part-purchase/part-purchase-modal.component.html`, com: título **Liberação**; aria-label do X "Fechar sem liberar"; seção "Orçamento reprovado" com "Solução", "Valor total", "Garantia" e "Reprovado em" (`responseDate | date: 'dd/MM/yyyy'` ou "—"), sem "Custo da peça"; rodapé com "O aparelho já está pronto para retirada?", **Cancelar** e **Liberado** (`(click)="release()"`, spinner + "Registrando…") (depende de T010)
- [X] T012 [P] [US1] Criar `rejected-release-modal.component.scss` copiando `modals/part-purchase/part-purchase-modal.component.scss`
- [X] T013 [US1] Em `web/src/app/features/home/home.component.ts`: adicionar `'action'` ao final de `rejectedColumns`; importar `RejectedReleaseModalComponent`; criar `protected openRejectedReleaseDialog(id: number)` com as mesmas opções de `openPartPurchaseDialog`, recarregando com `this.loadRejectedServiceOrders()` quando o modal fechar com `true`. Em `home.component.html`, depois do `matColumnDef="rejectedAt"`, adicionar a coluna `action` (`<th ... class="cell-date">Ação</th>`) com `<button matButton="filled" (click)="openRejectedReleaseDialog(element.number)">LIBERADO</button>` (depende de T010)
- [X] T014 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: a liberação funciona de ponta a ponta.

---

## Phase 4: User Story 2 - Fechar sem liberar e tratar erros (Priority: P2)

**Goal**: cancelar não registra nada; erro mostra mensagem com o modal aberto; não há clique duplo.

**Independent Test**: quickstart.md §3, passos 3 e 6.

- [X] T015 [US2] Conferir em `rejected-release-modal.component.{ts,html}`: X e Cancelar com `[disabled]="isSending()"`; `release()` ignorando a chamada se `isSending()` já for `true`; `disableClose` ligado durante o envio e desligado no `finalize` (também em erro)
- [X] T016 [US2] Rodar `cd web && npx ng build`

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T017 [P] Executar quickstart.md §2 (a rota e o command antigos não existem mais; a rota nova tem `RequireAuthorization()`)
- [ ] T018 Executar quickstart.md §3 manualmente, com a API e o Angular rodando e usuário logado
- [X] T019 [P] Remover imports não usados nos arquivos alterados e rodar os builds de novo

---

## Dependencies & Execution Order

- Setup → Foundational (fakes) → US1 → US2 → Polish
- US1 backend: T006 → T007 → T008; US1 frontend: T009 → T010 → T011 / T013; T012 em paralelo
- US2 depende da US1 (mesmo modal)

### Parallel Opportunities

- T003 ∥ T004 ∥ T005
- Backend da US1 (T006–T008) ∥ frontend da US1 (T009–T013)
- T009 ∥ T012
- T017 ∥ T019

## Parallel Example: User Story 1

```bash
Task: "T006–T008 backend (testes, handler, rota)"
Task: "T009 rejected-release-modal.service.ts"
Task: "T012 rejected-release-modal.component.scss"
```

## Implementation Strategy

1. Setup + fakes
2. US1 → **STOP and VALIDATE** (quickstart §3) → MVP
3. US2 → robustez
4. Polish; publicar frontend e API juntos (a rota mudou)
