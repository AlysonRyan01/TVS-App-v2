---

description: "Task list for 005-repair-modal"
---

# Tasks: Modal "Consertado" na aba "Aguardando peça"

**Input**: Design documents from `/specs/005-repair-modal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos, porque são exigidos pelo FR-010 e pelo Princípio IV. MSTest com os fakes de
`src/TVSApp.Tests/Handlers/Fakes/` (o `UpdateResponse` configurável já existe desde a 004).

**Organization**: US1 entrega o fluxo completo; US2 reforça cancelar e erros no modal.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Backend idêntico a `old-handlers/AddServiceOrderRepairHandler.cs` (ordem, mensagens, WhatsApp
  para todas as empresas, sem checar o salvamento, sem notificação interna).
- Sem MediatR; endpoint no formato
  `var result = await handler.X(id); if (result.IsSuccess) await hubContext.Clients.All.SendAsync("Atualizar", result.Message); return result.ToResult();`
  com `.WithTags("ServiceOrder").RequireAuthorization()`.
- Textos visíveis em português, exatamente como na spec. Sem migration.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (40 testes) e `cd web && npx ng build` e confirmar que tudo passa
- [X] T002 [P] Criar a pasta `web/src/app/features/home/modals/repair/`

---

## Phase 2: Foundational

- [X] T003 [P] Criar `src/TVSApp.Tests/Domain/ServiceOrderRepairTests.cs` (MSTest): (a) OS de `ServiceOrderFactory.Create()` + `AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg")` + `ApproveEstimate()` + `AddPurchasedPart()`, depois `ExecuteRepair()` → `ServiceOrderStatus == EServiceOrderStatus.Repaired` e `RepairDate` não nulo; (b) OS só com `AddEstimate` (aguardando resposta) → `EntityException<ServiceOrder>` com mensagem "Não podemos executar o conserto pois a ordem de serviço não tem o status de aprovado"

**Checkpoint**: `dotnet test` passa (42 testes).

---

## Phase 3: User Story 1 - Registrar o conserto (Priority: P1) 🎯 MVP

**Goal**: **CONSERTADO** abre o modal; **Consertado** registra o conserto, envia o WhatsApp e move a
OS para "Prontos".

**Independent Test**: quickstart.md §3, passos 1, 2 e 4.

### Tests for User Story 1

- [X] T004 [US1] Em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, adicionar (helper `AwaitingPart(EEnterprise)` = `AwaitingResponse(enterprise)` + `ApproveEstimate()` + `AddPurchasedPart()`): (a) `RegisterRepair_NotFound_Returns404` → `Code` 404, "Essa ordem de serviço não existe", `LastUpdated` nulo; (b) `RegisterRepair_EstimateNotApproved_Throws` (OS de `AwaitingResponse()`) → `Assert.ThrowsExceptionAsync<EntityException<ServiceOrder>>`, `LastUpdated` nulo, 0 WhatsApp; (c) `[DataRow(Particular)] [DataRow(PhilcoBritania)] RegisterRepair_AwaitingPart_RepairsAndSendsWhatsApp` → `Code` 200, "Conserto adicionado com sucesso", `Data` não nulo, `ServiceOrderStatus == Repaired`, `RepairDate` não nulo, `LastUpdated` = a OS, 0 notificações, 1 `SendDeviceReadyMessageRequest` com `ServiceOrder == "1"`, `CustomerName == "JOÃO DA SILVA"` e `PhoneNumber == "41999999999"`; (d) `RegisterRepair_UpdateFails_StillReturns200AndSendsWhatsApp` (`_commandRepository.UpdateResponse = new(null, 500, "Erro ao salvar")`) → `Code` 200 e 1 WhatsApp (documenta que o handler antigo não checa o salvamento)

### Implementation for User Story 1

- [X] T005 [US1] Em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs`, adicionar `Task<BaseResponse<ServiceOrderDto?>> RegisterRepair(long id);`. Em `ServiceOrderCommandHandler.cs`, implementar na ordem de research.md R1: `GetByIdForUpdateAsync(id)` com `Data == null` → `(null, 404, "Essa ordem de serviço não existe")`; `serviceOrder.ExecuteRepair()` (sem try/catch); `await _commandRepository.UpdateAsync(serviceOrder)` sem checar o resultado; `_whatsAppQueue.Enqueue(new SendDeviceReadyMessageRequest(serviceOrder.Id.ToString(), serviceOrder.Customer.Name.CustomerName, serviceOrder.Customer.Phone.CustomerPhone))`; retornar `(serviceOrder.MapToServiceOrderDto(), 200, "Conserto adicionado com sucesso")`. Comentário apontando para o handler antigo. Rodar T004, que deve passar
- [X] T006 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, remover o endpoint `PUT /add-service-order-repair` inteiro (do `app.MapPut` até o `.RequireAuthorization();`) e adicionar `PUT /service-orders/{id:long}/repair` conforme `contracts/http-endpoints.md`. Apagar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddServiceOrderRepairCommand.cs` com `git rm` (depende de T005)
- [X] T007 [P] [US1] Criar `web/src/app/features/home/modals/repair/repair-modal.service.ts` copiando `modals/rejected-release/rejected-release-modal.service.ts`: classe `RepairModalService`, método `registerRepair(id)` → `PUT ${environment.apiUrl}/service-orders/${id}/repair` com body `{}` (no lugar de `markAsReadyForPickup`)
- [X] T008 [US1] Criar `repair-modal.component.ts` copiando `modals/rejected-release/rejected-release-modal.component.ts`: classe `RepairModalComponent`, selector `app-repair-modal`, `RepairModalService`, `release()` → `registerRepair()`, chamando `registerRepair(this.id)`, com a mensagem de sucesso "Conserto registrado." e o fallback de erro "Não foi possível registrar o conserto. Tente de novo." (depende de T007)
- [X] T009 [US1] Criar `repair-modal.component.html` copiando `modals/rejected-release/rejected-release-modal.component.html`, com: título **Conserto**; aria-label do X "Fechar sem registrar"; seção "Resumo do conserto" com "Solução", "Valor total", "Garantia" e "Peça comprada em" (`purchasePartDate | date: 'dd/MM/yyyy'` ou "—") no lugar de "Reprovado em"; rodapé "O conserto já foi concluído?", **Cancelar** e **Consertado** (`(click)="registerRepair()"`) (depende de T008)
- [X] T010 [P] [US1] Criar `repair-modal.component.scss` copiando `modals/rejected-release/rejected-release-modal.component.scss`
- [X] T011 [US1] Em `web/src/app/features/home/home.component.ts`: adicionar `'action'` ao final de `awaitingPartColumns`; importar `RepairModalComponent`; criar `protected openRepairDialog(id: number)` com as mesmas opções de `openRejectedReleaseDialog`, recarregando com `this.loadAwaitingPartServiceOrders()` quando o modal fechar com `true`. Em `home.component.html`, depois do `matColumnDef="partPurchasedAt"`, adicionar a coluna `action` (`<th ... class="cell-date">Ação</th>`) com `<button matButton="filled" (click)="openRepairDialog(element.number)">CONSERTADO</button>` (depende de T008)
- [X] T012 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: o registro do conserto funciona de ponta a ponta.

---

## Phase 4: User Story 2 - Fechar sem registrar e tratar erros (Priority: P2)

**Goal**: cancelar não registra nada; erro mostra mensagem com o modal aberto; não há clique duplo.

**Independent Test**: quickstart.md §3, passos 3 e 5.

- [X] T013 [US2] Conferir em `repair-modal.component.{ts,html}`: X e Cancelar com `[disabled]="isSending()"`; `registerRepair()` ignorando a chamada se `isSending()` já for `true`; `disableClose` ligado durante o envio e desligado no `finalize` (também em erro)
- [X] T014 [US2] Rodar `cd web && npx ng build`

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T015 [P] Executar quickstart.md §2 (a rota e o command antigos não existem mais; a rota nova tem `RequireAuthorization()`)
- [ ] T016 Executar quickstart.md §3 manualmente, com a API e o Angular rodando e usuário logado
- [X] T017 [P] Remover imports não usados nos arquivos alterados e rodar os builds de novo

---

## Dependencies & Execution Order

- Setup → Foundational → US1 → US2 → Polish
- US1 backend: T004 → T005 → T006; US1 frontend: T007 → T008 → T009 / T011; T010 em paralelo
- US2 depende da US1 (mesmo modal)

### Parallel Opportunities

- T002 ∥ T001; T003 ∥ T004
- Backend da US1 (T004–T006) ∥ frontend da US1 (T007–T011)
- T007 ∥ T010; T015 ∥ T017

## Parallel Example: User Story 1

```bash
Task: "T004–T006 backend (testes, handler, rota)"
Task: "T007 repair-modal.service.ts"
Task: "T010 repair-modal.component.scss"
```

## Implementation Strategy

1. Setup + Foundational
2. US1 → **STOP and VALIDATE** (quickstart §3) → MVP
3. US2 → robustez
4. Polish; publicar frontend e API juntos (a rota mudou)
