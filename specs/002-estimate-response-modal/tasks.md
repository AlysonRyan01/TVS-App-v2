---

description: "Task list for 002-estimate-response-modal"
---

# Tasks: Modal de resposta do orçamento

**Input**: Design documents from `/specs/002-estimate-response-modal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos, porque são exigidos pelo FR-014 e pelo Princípio IV da constituição. MSTest
com fakes escritos à mão, no padrão da feature 001.

**Organization**: a Fase 2 monta a base comum (regra de domínio, middleware, handler, modal e
coluna). Depois, US1 (Aprovado), US2 (Reprovado) e US3 (cancelar/erros) adicionam cada fluxo.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1, US2, US3 (ver spec.md)

## Regras gerais para todas as tarefas

- Sem MediatR no fluxo novo: handler comum com interface, já registrado no DI
  (`IServiceOrderCommandHandler`).
- Endpoints: `var result = await handler.X(id); if (result.IsSuccess) await hubContext.Clients.All.SendAsync("Atualizar", result.Message); return result.ToResult();`
  com `.WithTags("ServiceOrder").RequireAuthorization()`.
- Textos visíveis em português, exatamente como na spec/contract.
- Sem migration.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build` na raiz e confirmar que tudo passa (base da feature 001: 12 testes)
- [X] T002 [P] Criar a pasta `src/TVSApp.Tests/Domain/` e a pasta `web/src/app/features/home/modals/estimate-response/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: regra de domínio, correção do status HTTP de erro, dependências do handler e o modal
"esqueleto" (somente leitura + Cancelar), que é compartilhado por US1, US2 e US3.

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase.

### Tests (escrever primeiro e ver falhar)

- [X] T003 [P] Criar `src/TVSApp.Tests/Domain/ServiceOrderEstimateResponseTests.cs` (MSTest) usando `ServiceOrderFactory.Create()` de `src/TVSApp.Tests/Handlers/Fakes/ServiceOrderFactory.cs` + `AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg")` para chegar a Evaluated/Waiting. Casos: (a) `IsAwaitingEstimateResponse` é `false` logo após criar (Entered/Entered) e `true` após `AddEstimate` com `Repair` (empresa `Particular`); (b) `ApproveEstimate()` → `RepairStatus == ERepairStatus.Approved` e `ResponseDate` não nulo; (c) `RejectEstimate()` → `RepairStatus == ERepairStatus.Disapproved` e `ResponseDate` não nulo; (d) chamar `ApproveEstimate()` ou `RejectEstimate()` numa OS já aprovada ou reprovada lança `EntityException<ServiceOrder>` com mensagem "Esse orçamento já foi respondido."; (e) `ApproveEstimate()` numa OS sem orçamento (Entered) lança `EntityException<ServiceOrder>`

### Implementation

- [X] T004 Em `src/TVSApp.Domain/Entities/ServiceOrder.cs`: adicionar `public bool IsAwaitingEstimateResponse => ServiceOrderStatus == EServiceOrderStatus.Evaluated && RepairStatus == ERepairStatus.Waiting;`; em `ApproveEstimate()` e `RejectEstimate()`, depois da validação de solução que já existe, adicionar `if (!IsAwaitingEstimateResponse) throw new EntityException<ServiceOrder>("Esse orçamento já foi respondido.");` (sem outras mudanças). Rodar T003, que deve passar
- [X] T005 [P] Em `src/TVSApp.Api/Middlewares/ExceptionMiddleware.cs`, depois do `switch` que define `code`/`message`, adicionar `context.Response.StatusCode = code;` antes de serializar a resposta (R6: hoje erros saem como HTTP 200)
- [X] T006 [P] Criar `src/TVSApp.Tests/Handlers/Fakes/FakeNotificationCommandRepository.cs` implementando `INotificationCommandRepository` (namespace `TVS_App.Domain.Repositories.Notifications`), guardando cada chamada `Create(title, message)` em `List<(string Title, string Message)> Created` e retornando `Task.FromResult(new BaseResponse<Notification>(null))`; implementar os demais membros da interface com `throw new NotImplementedException()`
- [X] T007 Em `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs`, adicionar `INotificationCommandRepository` ao construtor (campo `_notificationRepository`); em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, atualizar o `Setup` para passar `new FakeNotificationCommandRepository()` (guardar em `_notificationRepository`). Os 6 testes atuais continuam passando (depende de T006)
- [X] T008 [P] Criar `web/src/app/features/home/modals/estimate-response/estimate-response-modal.service.ts` (`@Service()` como o `AddServiceOrderEstimateModalService`), com `getServiceOrderById(id: number)` → `GET ${environment.apiUrl}/get-service-order-by-id/${id}`; `approveEstimate(id: number)` → `PUT ${environment.apiUrl}/service-orders/${id}/estimate/approve` com body `{}`; `rejectEstimate(id: number)` → `PUT ${environment.apiUrl}/service-orders/${id}/estimate/reject` com body `{}`. Todos retornam `Observable<BaseResponse<ServiceOrder>>`
- [X] T009 Criar `web/src/app/features/home/modals/estimate-response/estimate-response-modal.component.ts` (selector `app-estimate-response-modal`, classe `EstimateResponseModalComponent`) seguindo `add-service-order-estimate-modal.component.ts`: injeta `MatDialogRef`, `EstimateResponseModalService`, `SnackbarService` e `MAT_DIALOG_DATA` (`id: number`); signals `isLoading`, `isSending`, `serviceOrder`; `loadServiceOrder()` igual ao do modal de orçamento, sem `prefill`. Imports: `CurrencyPipe`, `EnumLabelPipe`, `MatDialogModule`, `MatButtonModule`, `MatProgressSpinnerModule`. Sem formulário (depende de T008)
- [X] T010 Criar `web/src/app/features/home/modals/estimate-response/estimate-response-modal.component.html` seguindo `add-service-order-estimate-modal.component.html`: header `bar` com título **Resposta** e botão fechar (`mat-dialog-close`, aria-label "Fechar sem responder"); estado de carregamento; `aside.ticket` idêntico (OS, nome, telefones, aparelho, defeito); no lugar do formulário, um bloco `section.estimate` somente leitura com rótulos em pt-BR: "Resultado" (`repairResult | enumLabel: 'repairResult'`), "Solução", "Valor total" (`totalAmount | currency: 'BRL' : 'symbol' : '1.2-2'`), "Garantia" (ou "—"), "Mensagem enviada" (`estimateMessage`); `footer.summary` com `div.actions` contendo por enquanto só **Cancelar** (`mat-dialog-close`); estado de erro idêntico ("Não foi possível carregar a OS {{ id }}. Confira a conexão e tente de novo.", com os botões Fechar / Tentar de novo) (depende de T009)
- [X] T011 [P] Criar `web/src/app/features/home/modals/estimate-response/estimate-response-modal.component.scss` copiando de `add-service-order-estimate-modal.component.scss` apenas as regras usadas no template de T010 (`:host`, `.bar`, `.state`, `.body`, `.columns`, `.ticket*`, `.summary`, `.actions`, `.send`, `.sr-only`) e adicionando o estilo de `.estimate` (lista de rótulo/valor no mesmo tom do `.ticket-device`) e de `.reject` (botão com cor de alerta, `var(--mat-sys-error)`)
- [X] T012 Em `web/src/app/features/home/home.component.ts`: adicionar `'action'` ao final de `awaitingResponseColumns`; importar `EstimateResponseModalComponent`; criar `protected openEstimateResponseDialog(id: number)` com as mesmas opções de `openEstimateDialog` (tela cheia, `panelClass: 'fullscreen-dialog'`, `autoFocus: 'dialog'`) e, em `afterClosed()`, chamar `this.loadAwaitingResponseServiceOrders()` quando o resultado for `true`. Em `home.component.html`, na tabela de `awaitingResponseServiceOrders()`, adicionar `<ng-container matColumnDef="action">` com `<th ... class="cell-date">Ação</th>` e `<button matButton="filled" (click)="openEstimateResponseDialog(element.number)">RESPOSTA</button>` (mesmo markup da coluna Ação de "Orçamentos pendentes") (depende de T009)
- [X] T013 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: a coluna e o botão aparecem; o modal abre, mostra os dados e fecha com Cancelar.

---

## Phase 3: User Story 1 - Registrar aprovação do orçamento (Priority: P1) 🎯 MVP

**Goal**: o botão **Aprovado** registra a aprovação, cria a notificação, envia o WhatsApp
(exceto PhilcoBritania) e move a OS para "Aprovados".

**Independent Test**: quickstart.md §3, passo 5.

### Tests for User Story 1

- [X] T014 [P] [US1] Em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, adicionar testes de `ApproveEstimate(long id)` (OS preparada com `ServiceOrderFactory.Create()` + `AddEstimate(...)` com `ERepairResult.Repair`): (a) OS inexistente → `Code` 404, "Essa ordem de serviço não existe"; (b) OS já aprovada → `Code` 409, "Esse orçamento já foi respondido.", sem `UpdateAsync`, sem notificação e sem WhatsApp; (c) sucesso → `Code` 200, "Aprovação adicionada com sucesso!", `RepairStatus == Approved`, `UpdateAsync` chamado, 1 notificação ("Orçamento Aprovado, OS: 1", "Favor comprar peça para OS: 1"); (d) empresa `Particular` → 1 `SendThankYouForEstimateApprovalRequest` com `ServiceOrder == "1"`, `CustomerName == "JOÃO DA SILVA"`, `PhoneNumber == "41999999999"`, `Amount == serviceOrder.TotalAmount`; (e) `EEnterprise.PhilcoBritania` → nenhum WhatsApp (preparar a OS PhilcoBritania com `AddEstimate` e forçar Evaluated/Waiting via `EditServiceOrderStatus(EServiceOrderStatus.Evaluated)` + `EditRepairStatus(ERepairStatus.Waiting)`, porque na PhilcoBritania o `AddEstimate` já aprova)

### Implementation for User Story 1

- [X] T015 [US1] Em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs`, adicionar `Task<BaseResponse<ServiceOrderDto?>> ApproveEstimate(long id);`. Em `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs`, implementar: `GetByIdForUpdateAsync(id)` (se `Data == null` → 404 "Essa ordem de serviço não existe"); se `!serviceOrder.IsAwaitingEstimateResponse` → `new BaseResponse<ServiceOrderDto?>(null, 409, "Esse orçamento já foi respondido.")`; `serviceOrder.ApproveEstimate()`; `UpdateAsync`; `_notificationRepository.Create($"Orçamento Aprovado, OS: {id}", $"Favor comprar peça para OS: {id}")`; se `Enterprise != EEnterprise.PhilcoBritania`, `Enqueue(new SendThankYouForEstimateApprovalRequest(serviceOrder.Id.ToString(), serviceOrder.Customer.Name.CustomerName, serviceOrder.Customer.Phone.CustomerPhone, serviceOrder.TotalAmount))`; retornar `BaseResponse<ServiceOrderDto?>(serviceOrder.MapToServiceOrderDto(), 200, "Aprovação adicionada com sucesso!")`. Rodar T014, que deve passar
- [X] T016 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, remover o endpoint `PUT /add-service-order-approve-estimate` inteiro e adicionar `app.MapPut("/service-orders/{id:long}/estimate/approve", async (IServiceOrderCommandHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) => { ... })` no formato das regras gerais. Apagar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddServiceOrderApproveEstimateCommand.cs` (`git rm`) e remover usings órfãos (depende de T015)
- [X] T017 [US1] Em `estimate-response-modal.component.ts`, adicionar `protected approve()`, que: `isSending.set(true)`; chama `approveEstimate(this.id)` com `finalize(() => isSending.set(false))`; em `next`, `_snackbar.open("Orçamento aprovado.")` e `_dialogRef.close(true)`; em `error`, mostra `error.error?.message ?? (typeof error.error === 'string' ? error.error : 'Não foi possível registrar a resposta. Tente de novo.')`. Em `estimate-response-modal.component.html`, adicionar ao `div.actions` o botão `matButton="filled"` **Aprovado** (`class="send"`, `type="button"`, `(click)="approve()"`, `[disabled]="isSending()"`) depois do Cancelar
- [X] T018 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: aprovar funciona de ponta a ponta.

---

## Phase 4: User Story 2 - Registrar reprovação do orçamento (Priority: P1)

**Goal**: o botão **Reprovado** registra a reprovação, cria a notificação, envia o WhatsApp
(exceto PhilcoBritania) e move a OS para "Reprovados".

**Independent Test**: quickstart.md §3, passo 6.

### Tests for User Story 2

- [X] T019 [P] [US2] Em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, adicionar os mesmos casos de T014 para `RejectEstimate(long id)`: 404; 409 "Esse orçamento já foi respondido."; sucesso → 200 "Orçamento reprovado com sucesso!", `RepairStatus == Disapproved`, notificação ("Orçamento Reprovado, OS: 1", "Favor montar a OS 1 para entrega"); `Particular` → 1 `SendEstimateDisapprovalAcknowledgmentRequest("1", "JOÃO DA SILVA", "41999999999")`; PhilcoBritania → nenhum WhatsApp

### Implementation for User Story 2

- [X] T020 [US2] Em `IServiceOrderCommandHandler.cs`, adicionar `Task<BaseResponse<ServiceOrderDto?>> RejectEstimate(long id);`. Em `ServiceOrderCommandHandler.cs`, implementar igual a T015, trocando: `serviceOrder.RejectEstimate()`; notificação `($"Orçamento Reprovado, OS: {id}", $"Favor montar a OS {id} para entrega")`; WhatsApp `new SendEstimateDisapprovalAcknowledgmentRequest(serviceOrder.Id.ToString(), serviceOrder.Customer.Name.CustomerName, serviceOrder.Customer.Phone.CustomerPhone)`; mensagem "Orçamento reprovado com sucesso!". Extrair o trecho comum (buscar, checar 404/409, salvar, mapear) para um método privado e evitar duplicação. Rodar T019, que deve passar
- [X] T021 [US2] Em `ServiceOrderEndpoints.cs`, remover `PUT /add-service-order-reject-estimate` e adicionar `PUT /service-orders/{id:long}/estimate/reject` no mesmo formato de T016. Apagar `AddServiceOrderRejectEstimateCommand.cs` (`git rm`) (depende de T020)
- [X] T022 [US2] Em `estimate-response-modal.component.ts`, adicionar `protected reject()` igual a `approve()`, chamando `rejectEstimate(this.id)` com snackbar "Orçamento reprovado.". Para não duplicar, extrair um método privado `respond(request$, successMessage)`. Em `estimate-response-modal.component.html`, adicionar entre Cancelar e Aprovado o botão `matButton` **Reprovado** (`class="reject"`, `type="button"`, `(click)="reject()"`, `[disabled]="isSending()"`)
- [X] T023 [US2] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: aprovar e reprovar funcionam.

---

## Phase 5: User Story 3 - Fechar sem responder e tratar erros (Priority: P2)

**Goal**: cancelar não registra nada; erros mostram mensagem e mantêm o modal aberto; clique
duplo é bloqueado.

**Independent Test**: quickstart.md §3, passos 4, 7 e 8.

- [X] T024 [US3] Em `estimate-response-modal.component.html`: aplicar `[disabled]="isSending()"` também no **Cancelar** do rodapé; mostrar `<mat-spinner diameter="18">` dentro do botão clicado enquanto `isSending()` (guardar qual ação está em andamento em um signal `sendingAction = signal<'approve' | 'reject' | null>(null)` no `.ts`); manter `disableClose` do dialog enquanto envia (`this._dialogRef.disableClose = true/false` em `respond`)
- [X] T025 [US3] Em `estimate-response-modal.component.ts`, no `error` de `respond`, se `error.status === 409`, além da mensagem, marcar `answeredElsewhere = true`; no fechamento do modal (Cancelar/X/Esc), fechar com `this._dialogRef.close(answeredElsewhere)` para a Home recarregar a lista (edge case "OS já respondida"). Usar `[mat-dialog-close]="answeredElsewhere()"` nos botões de fechar
- [X] T026 [US3] Rodar `cd web && npx ng build`

**Checkpoint**: todas as stories estão funcionando.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T027 [P] Executar quickstart.md §2 (as rotas antigas não existem mais; as novas têm `RequireAuthorization()`)
- [ ] T028 Executar quickstart.md §3 e §4 manualmente, com a API e o Angular rodando e usuário logado
- [X] T029 [P] Remover imports não usados nos arquivos alterados e rodar `cd web && npx ng build` e `dotnet build TVSApp.sln` de novo

---

## Dependencies & Execution Order

- **Setup (Phase 1)** → **Foundational (Phase 2)** → **US1 (Phase 3)** → **US2 (Phase 4)** → **US3 (Phase 5)** → **Polish (Phase 6)**
- US1 e US2 mexem nos mesmos arquivos (handler, endpoints, modal), então são feitas em sequência.
  US2 pode vir antes de US1 sem problema.
- US3 depende de US1 ou US2 (precisa de pelo menos uma ação para testar erros).

### Within Each Phase

- Teste → Domain → Application → Api → Angular → build

### Parallel Opportunities

- T002 ∥ T001
- T003 ∥ T005 ∥ T006 ∥ T008 ∥ T011 (arquivos diferentes)
- T014 (testes US1) ∥ T017 (frontend US1)
- T019 (testes US2) ∥ T022 (frontend US2)
- T027 ∥ T029

---

## Parallel Example: Foundational

```bash
Task: "T003 testes de domínio em src/TVSApp.Tests/Domain/ServiceOrderEstimateResponseTests.cs"
Task: "T005 StatusCode no ExceptionMiddleware"
Task: "T006 FakeNotificationCommandRepository"
Task: "T008 estimate-response-modal.service.ts"
Task: "T011 estimate-response-modal.component.scss"
```

---

## Implementation Strategy

### MVP First

1. Phase 1 + Phase 2: modal visível, somente leitura
2. Phase 3 (US1): aprovar
3. **STOP and VALIDATE**: quickstart §3 passo 5
4. Publicar frontend e API juntos

### Incremental Delivery

1. Base → modal abre
2. US1 → aprovar
3. US2 → reprovar
4. US3 → robustez (cancelar, 409, erros, clique duplo)

---

## Notes

- A mudança de T005 afeta o status HTTP de erros em toda a API (R6).
- Commitar ao fim de cada fase.

> **Revisão 2026-11-05**: os efeitos de WhatsApp de T014/T015/T019/T020 foram alinhados aos handlers antigos (`old-handlers/`): aviso ao dono nos dois casos; reprovação também envia na PhilcoBritania; mensagem da aprovação "Aprovação adicionada com sucesso!". Ver spec.md (Clarifications).
