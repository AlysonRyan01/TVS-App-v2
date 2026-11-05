---

description: "Task list for 006-delivery-modal"
---

# Tasks: Modal "Entregue" na aba "Prontos"

**Input**: Design documents from `/specs/006-delivery-modal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos, porque são exigidos pelo FR-011 e pelo Princípio IV. MSTest com os fakes de
`src/TVSApp.Tests/Handlers/Fakes/`, mais o novo `FakeServiceOrderPdfGenerator`.

**Organization**: a Fase 2 prepara a dependência de PDF. US1 entrega o fluxo completo para OS
consertada (PDF e WhatsApp). US2 cobre OS não consertada. US3 reforça cancelar e erros.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Backend idêntico a `old-handlers/AddServiceOrderDeliveryAndReturnPdfHandler.cs` (salva antes
  do PDF, 500 se o PDF falhar, WhatsApp de garantia para todas as empresas só quando há PDF,
  sem notificação interna).
- Sem MediatR. Endpoint conforme `contracts/http-endpoints.md` (arquivo quando há PDF; senão
  `ToResult()`), com `.WithTags("ServiceOrder").RequireAuthorization()`.
- Textos visíveis em português, exatamente como na spec. Sem migration.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (47 testes) e `cd web && npx ng build` e confirmar que tudo passa
- [X] T002 [P] Criar a pasta `web/src/app/features/home/modals/delivery/`

---

## Phase 2: Foundational

**⚠️ CRITICAL**: muda o construtor do handler; os testes atuais precisam continuar passando.

- [X] T003 [P] Criar `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderPdfGenerator.cs` implementando `IGenerateServiceOrderPdf` (`TVS_App.Application.Interfaces.Services`): `public BaseResponse<byte[]> CheckOutResponse { get; set; } = new(new byte[] { 0x25, 0x50, 0x44, 0x46 }, 200, "PDF gerado")`; `public int CheckOutCalls { get; private set; }`; `GenerateCheckOutDocumentAsync` incrementa o contador e retorna `CheckOutResponse`; os outros métodos lançam `NotImplementedException`
- [X] T004 Em `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs`, adicionar `IGenerateServiceOrderPdf pdfGenerator` como último parâmetro do construtor (campo `_pdfGenerator`). Em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`, criar `_pdfGenerator = new FakeServiceOrderPdfGenerator()` no `Setup` e passá-lo ao handler. Os 47 testes continuam passando (depende de T003)
- [X] T005 [P] Criar `src/TVSApp.Tests/Domain/ServiceOrderDeliveryTests.cs`: `ServiceOrderFactory.Create()` → `AddDelivery()` → `ServiceOrderStatus == EServiceOrderStatus.Delivered` e `DeliveryDate` não nulo
- [X] T006 [P] Em `web/src/app/shared/models/service-order.interface.ts`, corrigir `repairStatus: ERepairResult;` para `repairStatus: ERepairStatus;` e importar `ERepairStatus` (R4)

**Checkpoint**: `dotnet test` passa (48 testes) e `ng build` passa.

---

## Phase 3: User Story 1 - Entregar aparelho consertado com termo de entrega (Priority: P1) 🎯 MVP

**Goal**: **ENTREGUE** abre o modal; **Entregue** numa OS consertada registra a entrega, abre o PDF
numa nova aba (ou baixa) e envia o WhatsApp de garantia.

**Independent Test**: quickstart.md §3, passos 1, 2, 4 e 5.

### Tests for User Story 1

- [X] T007 [US1] Em `ServiceOrderCommandHandlerTests.cs`, adicionar (helper `RepairedOs(EEnterprise)` = `AwaitingPart(enterprise)` + `ExecuteRepair()`): (a) `RegisterDelivery_NotFound_Returns404` → `Code` 404, "Essa ordem de serviço não existe", `_pdfGenerator.CheckOutCalls == 0`; (b) `[DataRow(Particular)] [DataRow(PhilcoBritania)] RegisterDelivery_Repaired_ReturnsPdfAndSendsGuarantee` → `Code` 200, `Data` igual aos bytes do fake, `ServiceOrderStatus == Delivered`, `DeliveryDate` não nulo, `LastUpdated` = a OS, `CheckOutCalls == 1`, 0 notificações, 1 `SendGuaranteeMessageRequest` com `ServiceOrder == "1"`, `CustomerName == "JOÃO DA SILVA"`, `Guarantee == "90 DIAS"`, `PhoneNumber == "41999999999"`; (c) `RegisterDelivery_Repaired_PdfFails_Returns500AndKeepsDelivery` (`_pdfGenerator.CheckOutResponse = new(null, 500, "Falha no PDF")`) → `Code` 500, `Message == "Falha no PDF"`, `ServiceOrderStatus == Delivered`, `LastUpdated` = a OS, 0 WhatsApp (depende de T004)

### Implementation for User Story 1

- [X] T008 [US1] Em `IServiceOrderCommandHandler.cs`, adicionar `Task<BaseResponse<byte[]>> RegisterDelivery(long id);`. Em `ServiceOrderCommandHandler.cs`, implementar conforme research.md R1: `GetByIdForUpdateAsync(id)` com `Data == null` → `new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe")`; `serviceOrder.AddDelivery()`; `await _commandRepository.UpdateAsync(serviceOrder)` sem checar; se `serviceOrder.RepairStatus == ERepairStatus.Approved && serviceOrder.RepairResult == ERepairResult.Repair` → `var pdf = await _pdfGenerator.GenerateCheckOutDocumentAsync(serviceOrder)`; se `!pdf.IsSuccess` → `new BaseResponse<byte[]>(null, 500, pdf.Message)`; `_whatsAppQueue.Enqueue(new SendGuaranteeMessageRequest(serviceOrder.Id.ToString(), serviceOrder.Customer.Name.CustomerName, serviceOrder.Guarantee!.ServiceOrderGuarantee, serviceOrder.Customer.Phone.CustomerPhone))`; `return pdf`; senão → `new BaseResponse<byte[]>(null, 200, "Ordem de serviço entregue com sucesso!")`. Comentário apontando para o handler antigo. Rodar T007, que deve passar
- [X] T009 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, remover o endpoint `PUT /add-service-order-delivery` inteiro (do `app.MapPut` até o `.RequireAuthorization();`) e adicionar `PUT /service-orders/{id:long}/delivery` exatamente como em `contracts/http-endpoints.md`. Apagar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddServiceOrderDeliveryAndReturnPdfCommand.cs` com `git rm` (depende de T008)
- [X] T010 [P] [US1] Criar `web/src/app/features/home/modals/delivery/delivery-modal.service.ts` (`@Service()`, classe `DeliveryModalService`): `getServiceOrderById(id)` igual aos outros modais; `registerDelivery(id): Observable<HttpResponse<Blob>>` → `this._http.put(\`${environment.apiUrl}/service-orders/${id}/delivery\`, {}, { observe: 'response', responseType: 'blob' })`
- [X] T011 [US1] Criar `delivery-modal.component.ts` (selector `app-delivery-modal`, classe `DeliveryModalComponent`) partindo de `modals/repair/repair-modal.component.ts`, com: `protected isRepaired = computed(() => { const o = this.serviceOrder(); return !!o && o.repairStatus === ERepairStatus.Approved && o.repairResult === ERepairResult.Repair; })`; `protected registerDelivery()` que (1) ignora se `isSending()`; (2) **antes** da requisição, se `isRepaired()`, faz `const pdfWindow = window.open('', '_blank')`; (3) trava (`isSending`, `disableClose`); (4) chama `registerDelivery(this.id)`; em `next(response)`: se `response.body` existir e `response.headers.get('content-type')?.includes('application/pdf')`, cria `URL.createObjectURL(response.body)` e, se `pdfWindow` existir, `pdfWindow.location.href = url`, senão baixa via `<a href=url download="ordem_servico_{id}.pdf">` + `click()`; revoga a URL com `setTimeout(..., 60000)`; se não houver PDF, `pdfWindow?.close()`; depois `_snackbar.open("Ordem de serviço entregue.")` e `_dialogRef.close(true)`; em `error`: `pdfWindow?.close()` e mostrar a mensagem lida de `error.error` (se for `Blob`, `JSON.parse(await error.error.text()).message`), com fallback "Não foi possível registrar a entrega. Tente de novo."; (5) destrava no `finalize` (depende de T010)
- [X] T012 [US1] Criar `delivery-modal.component.html` partindo de `modals/repair/repair-modal.component.html`, com: título **Entrega**; aria-label do X "Fechar sem entregar"; seção "Resumo da entrega" com "Resultado" (`repairResult | enumLabel: 'repairResult'` ou "—"), "Solução", "Valor total", "Garantia", "Consertado em" (`repairDate | date: 'dd/MM/yyyy'` ou "—"); rodapé com "O cliente já retirou o aparelho?" e, quando `isRepaired()`, a dica "O termo de entrega será aberto para impressão."; botões **Cancelar** e **Entregue** (`(click)="registerDelivery()"`) (depende de T011)
- [X] T013 [P] [US1] Criar `delivery-modal.component.scss` copiando `modals/repair/repair-modal.component.scss` e adicionando `.hint` (texto pequeno `$ink-muted`) para a dica do PDF
- [X] T014 [US1] Em `web/src/app/features/home/home.component.ts`: adicionar `'action'` ao final de `readyForPickupColumns`; importar `DeliveryModalComponent`; criar `protected openDeliveryDialog(id: number)` com as mesmas opções de `openRepairDialog`, recarregando com `this.loadReadyForPickupServiceOrders()` quando o modal fechar com `true`. Em `home.component.html`, depois do `matColumnDef="repairStatus"`, adicionar a coluna `action` (`<th ... class="cell-date">Ação</th>`) com `<button matButton="filled" (click)="openDeliveryDialog(element.number)">ENTREGUE</button>` (depende de T011)
- [X] T015 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: a entrega de OS consertada funciona com o PDF.

---

## Phase 4: User Story 2 - Entregar aparelho sem conserto (Priority: P1)

**Goal**: OS não consertada é entregue sem PDF e sem WhatsApp.

**Independent Test**: quickstart.md §3, passo 6.

- [X] T016 [US2] Em `ServiceOrderCommandHandlerTests.cs`, adicionar `RegisterDelivery_NotRepaired_DeliversWithoutPdf` (OS de `Rejected()` + `MarkEstimateRejectedAsReadyToPickUp()`) → `Code` 200, "Ordem de serviço entregue com sucesso!", `Data` nulo, `ServiceOrderStatus == Delivered`, `CheckOutCalls == 0`, 0 WhatsApp
- [X] T017 [US2] Conferir em `delivery-modal.component.ts` que, sem PDF (OS não consertada), nenhuma aba é aberta (`isRepaired()` falso) e o fluxo segue para a mensagem e o fechamento. Rodar `dotnet test src/TVSApp.Tests`

---

## Phase 5: User Story 3 - Fechar sem entregar e tratar erros (Priority: P2)

**Goal**: cancelar não registra nada; erro mostra mensagem com o modal aberto e fecha a aba vazia;
não há clique duplo.

**Independent Test**: quickstart.md §3, passos 3 e 7.

- [X] T018 [US3] Conferir em `delivery-modal.component.{ts,html}`: X e Cancelar com `[disabled]="isSending()"`; `registerDelivery()` ignorando a chamada se `isSending()`; `disableClose` ligado durante o envio e desligado no `finalize`; `pdfWindow?.close()` no erro; leitura da mensagem de erro que vem como `Blob`
- [X] T019 [US3] Rodar `cd web && npx ng build`

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T020 [P] Executar quickstart.md §2 (a rota e o command antigos não existem mais; a rota nova tem `RequireAuthorization()`)
- [ ] T021 Executar quickstart.md §3 manualmente, com a API e o Angular rodando e usuário logado (incluindo o teste com pop-up bloqueado)
- [X] T022 [P] Remover imports não usados nos arquivos alterados e rodar os builds de novo

---

## Dependencies & Execution Order

- Setup → Foundational (fake PDF, construtor, tipo do frontend) → US1 → US2 → US3 → Polish
- US1 backend: T007 → T008 → T009; US1 frontend: T010 → T011 → T012 / T014; T013 em paralelo
- US2 e US3 dependem da US1 (mesmo handler e mesmo modal)

### Parallel Opportunities

- T003 ∥ T005 ∥ T006
- Backend da US1 (T007–T009) ∥ frontend da US1 (T010–T014)
- T010 ∥ T013; T020 ∥ T022

## Parallel Example: User Story 1

```bash
Task: "T007–T009 backend (testes, handler, rota com PDF)"
Task: "T010 delivery-modal.service.ts"
Task: "T013 delivery-modal.component.scss"
```

## Implementation Strategy

1. Setup + Foundational
2. US1 → **STOP and VALIDATE** (quickstart §3, passos 4 e 5) → MVP
3. US2 → entrega sem PDF
4. US3 → robustez
5. Polish; publicar frontend e API juntos (a rota mudou)
