# Tasks: Funções gerais

**Input**: Design documents from `/specs/012-general-functions/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos — a constituição (IV) e a FR-007 exigem testes de Alterar e Reimprimir.

**Organization**: por user story (US1 Reimprimir, US2 Alterar OS, US3 Localização, US4 Clientes).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- Namespaces seguem o padrão existente (`TVS_App.*`; testes em `TVSApp.Tests.*`)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (107 verdes) e `cd web && npx ng build` (sem erro)

---

## Phase 2: Foundational (painel com rodapé de ações)

- [X] T002 Em `web/src/app/features/search-service-orders/service-order-details-dialog/service-order-details-dialog.component.{ts,html,scss}`: o rodapé passa a ter uma área de ações à esquerda do **Fechar** (onde entram Reimprimir e Alterar), e o diálogo recebe `MatDialogRef` para poder devolver a OS alterada no `close`; sem mudança visual além do rodapé

---

## Phase 3: User Story 1 — Reimprimir (P1) 🎯 MVP

**Goal**: botão Reimprimir no painel gera o PDF certo e abre para impressão.

**Independent Test**: quickstart §2 item 2.

- [X] T003 [P] [US1] Em `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderPdfGenerator.cs`, implementar `RegeneratePdfAsync(ServiceOrder)` com `RegenerateResponse` configurável (padrão `new(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x52 }, 200, "PDF gerado")`) e `RegenerateCalls`
- [X] T004 [US1] Testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` para `RegeneratePdf(id)`: OS inexistente → 404 "Essa ordem de serviço não existe" sem chamar o gerador; falha do gerador → 500 com a mensagem; sucesso → 200 com os bytes e `RegenerateCalls == 1`
- [X] T005 [US1] Implementar `Task<BaseResponse<byte[]>> RegeneratePdf(long id)` em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs` e `ServiceOrderCommandHandler.cs` (comentário citando `old-handlers/RegenerateAndReturnPdfHandler.cs`): `GetByIdForUpdateAsync` → 404 → `RegeneratePdfAsync` → 500 se falhar; T004 deve passar
- [X] T006 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`: adicionar `GET /service-orders/{id:long}/pdf` (erro → `result.ToResult()`; sucesso → `Results.File(pdf, "application/pdf", $"ordem_servico_{id}.pdf")`; `.WithTags("ServiceOrder").RequireAuthorization()`); remover `POST /regenerate-service-order-pdf` e apagar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/RegenerateAndReturnPdfCommand.cs`
- [X] T007 [P] [US1] Em `web/src/app/features/search-service-orders/search-service-orders.service.ts`: `regeneratePdf(id): Observable<HttpResponse<Blob>>` (`GET ${apiUrl}/service-orders/${id}/pdf`, `observe: 'response'`, `responseType: 'blob'`)
- [X] T008 [US1] No painel (`service-order-details-dialog.component.{ts,html}`): botão **Reimprimir** (outlined, ícone `print`) no rodapé; no clique `openPdfWindow()`, chama `regeneratePdf`, `showPdf(body, win, 'ordem_servico_{id}.pdf', 'OS {id}')`; erro → fecha a aba e snackbar com `readBlobErrorMessage(error, 'Não foi possível reimprimir. Tente de novo.')`; spinner e bloqueio contra clique duplo; o painel continua aberto

---

## Phase 4: User Story 2 — Alterar OS (P1)

**Goal**: editar os campos da OS no painel e salvar.

**Independent Test**: quickstart §2 itens 4–7.

### Backend

- [X] T009 [US2] Reescrever `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/EditServiceOrderCommand.cs` sem MediatR: `record EditServiceOrderCommand(string Brand, string Model, string SerialNumber, string? Defect, string? Accessories, EProduct Type, string? Location, EEnterprise Enterprise, DateTime? EntryDate, DateTime? InspectionDate, DateTime? ResponseDate, DateTime? RepairDate, DateTime? PurchasePartDate, DateTime? DeliveryDate, string? Solution, string? Guarantee, decimal PartCost, decimal LaborCost)`; `Validate()` com `CommandException<EditServiceOrderCommand>` e as mensagens de data-model.md ("O modelo da ordem de serviço não pode estar vazio", "O número de série da ordem de serviço não pode estar vazio", "Tipo de produto inválido.", "Empresa inválida.", "A data de entrada é obrigatória.", "O custo da peça não pode ser negativo.", "O custo de mão de obra não pode ser negativo."); `Normalize()` com `Trim().ToUpper()` nos textos (nulos → `""`)
- [X] T010 [US2] Testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` para `UpdateServiceOrder(id, command)`: cada regra de `Validate` lança `CommandException` sem gravar; OS inexistente → 404 "Essa ordem de serviço não existe"; sucesso → 200 "Ordem de serviço alterada com sucesso!", `LastUpdated` com marca/modelo/série/defeito/acessórios/tipo/empresa/datas/solução/garantia/peça/mão de obra novos e normalizados; **localização** gravada com o valor do command; **cliente, situação, status do orçamento, resultado e custo de compra de peça** iguais aos de antes; OS de venda (`Defect == "VENDA"`) mantém defeito "VENDA" e acessórios "" mesmo com outros valores no command
- [X] T011 [US2] Implementar `Task<BaseResponse<ServiceOrderDto?>> UpdateServiceOrder(long id, EditServiceOrderCommand command)` em `IServiceOrderCommandHandler.cs`/`ServiceOrderCommandHandler.cs` conforme research R1 e data-model.md (`Product.UpdateProduct` + `Product.AddLocation`; `EditEnterprise`; `EditEntryDate`; `EditInspectionDate`; `EditResponseDate`; `EditRepairDate`; `EditPurchasePartDate`; `EditDeliveryDate`; `EditSolution(new Solution(...))`; `EditGuarantee(new Guarantee(...))`; `EditPartCost`; `EditLaborCost`); comentário citando `old-handlers/EditServiceOrderHandler.cs` e as diferenças; T010 deve passar
- [X] T012 [US2] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`: adicionar `PUT /service-orders/{id:long}` (`handler.UpdateServiceOrder(id, command)`; sucesso → SignalR `"Atualizar"`; `ToResult()`); remover `PUT /edit-service-order`. Em `src/TVSApp.Application/Mappers/ServiceOrderMapper.cs` remover `ApplyChanges` e `MapToUpdateResponseDto`; apagar `src/TVSApp.Application/DTOs/ServiceOrder/UpdateServiceOrderResponseDto.cs` se ficar sem uso (o `UpdateServiceOrderCommand` antigo ainda o referencia: trocar o tipo de retorno dele para `ServiceOrderDto?` ou manter o DTO — escolher o que compila sem mudar comportamento)

### Frontend

- [X] T013 [P] [US2] Em `search-service-orders.service.ts`: `update(id, body): Observable<BaseResponse<ServiceOrder>>` (`PUT ${apiUrl}/service-orders/${id}`)
- [X] T014 [US2] No painel (`service-order-details-dialog.component.{ts,html,scss}`): `FormGroup` com os campos da OS (marca, modelo*, série*, defeito, acessórios, tipo, empresa, localização, entrada*, avaliação, resposta, compra de peça, conserto, entrega, solução, garantia, peça, mão de obra), preenchido a partir da OS; em leitura inputs `[readonly]="!editing()"` e selects (`mat-select` de tipo e empresa) desabilitados; datas em `input type="date"` (`yyyy-MM-dd`); total calculado (peça + mão de obra); dados do cliente sempre só leitura; OS de venda sem defeito/acessórios/solução
- [X] T015 [US2] Modo edição no painel: botão **Alterar** (rodapé) liga `editing`; rodapé vira **Cancelar · Salvar**; `dialogRef.disableClose = true` enquanto edita e Esc (`keydown.escape`) cancela; **Cancelar** restaura os valores da OS; **Salvar** bloqueado se inválido/enviando, envia `update`, snackbar "OS {id} alterada.", atualiza a OS do painel e volta a leitura; erro → snackbar com `error.error?.message`, continua editando
- [X] T016 [US2] Em `search-service-orders.component.ts`: ao fechar o diálogo com uma OS alterada (`afterClosed`), atualizar a linha correspondente em `results` (cliente, tipo, marca, modelo, série, empresa, entrada)

---

## Phase 5: User Story 3 — Localização (P2)

**Goal**: editar a prateleira direto no painel, sem entrar em Alterar.

**Independent Test**: quickstart §2 item 3.

- [X] T017 [P] [US3] Em `search-service-orders.service.ts`: `updateLocation(id, location): Observable<BaseResponse<string>>` (`PUT ${apiUrl}/service-orders/${id}/product/location` com `{ location }`, como os modais da Home)
- [X] T018 [US3] No painel: em modo leitura o campo **Localização** é editável; quando o valor muda aparece o botão de salvar (ícone `check`, `matSuffix`) e Enter salva; vazio bloqueia; sucesso → snackbar "Localização salva." e atualiza a OS do painel; em modo edição a localização vai junto do Salvar geral

---

## Phase 6: User Story 4 — Clientes (P2)

**Goal**: editar cliente sem criar OS; menu sem "Geral".

**Independent Test**: quickstart §2 itens 1 e 8.

- [X] T019 [P] [US4] Em `web/src/app/features/customers/customer-dialog/customer-dialog.component.{ts,html}`: `CustomerDialogData` ganha `mode?: 'confirm' | 'edit'` (padrão `'confirm'`, nada muda nas telas Adicionar OS/Vender); em `'edit'`: título "Editar cliente", texto "Altere os dados e salve.", botão "Salvar", e fechar sem alterações não devolve cliente
- [X] T020 [US4] Criar `web/src/app/features/customers/customers-page/customers-page.component.{ts,html,scss}` (`app-customers-page`, título "Clientes"): cartão centralizado com título "Editar cliente" e `app-customer-search`; ao escolher, abre `CustomerDialogComponent` com `mode: 'edit'`; ao salvar, snackbar "Cliente atualizado." e limpa a busca; visual dos cartões das telas atuais (fundo `$surface`, cartão branco, borda `#ead8d3`)
- [X] T021 [US4] Rota `{ path: 'clientes', loadComponent: () => import('./features/customers/customers-page/customers-page.component').then(m => m.CustomersPageComponent), canActivate: [authGuard] }` em `web/src/app/app.routes.ts`; em `web/src/app/shared/layout/base-layout/base-layout.component.ts` remover o item "Geral" e adicionar `{ label: 'Clientes', icon: 'group', route: '/clientes' }` depois de "Buscar OS"

---

## Phase 7: Polish & validação

- [X] T022 [P] `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` verdes; `grep -rn "edit-service-order\|regenerate-service-order-pdf" src web/src` vazio
- [X] T023 [P] `cd web && npx ng build` sem erro (inicial < 1 MB; SCSS de componente < 4 kB)
- [ ] T024 Roteiro manual do quickstart §2

---

## Dependencies & Execution Order

- T001 → T002 → US1 / US2 / US3 (frontend do painel). Backend de US1 e US2 só depende de T001.
- US1: T003 → T004 → T005 → T006; T007 → T008.
- US2: T009 → T010 → T011 → T012; T013 → T014 → T015 → T016.
- US3: T017 → T018 (depois de T014, que cria o form do painel).
- US4 é independente (T019 → T020 → T021).
- Painel (`service-order-details-dialog`): T002 → T008 → T014 → T015 → T018 (mesmo arquivo, em sequência).

### Parallel Opportunities

- Backend (T003–T006, T009–T012) ∥ frontend do painel; US4 inteira ∥ resto.
- T003 ∥ T007 ∥ T013 ∥ T017 ∥ T019; T022 ∥ T023.

### Parallel Example

```text
T003 fake Regenerate  |  T009 EditServiceOrderCommand  |  T019 modo 'edit' do modal de cliente  |  T007/T013/T017 serviço
```

## Implementation Strategy

1. **MVP**: Setup + T002 + US1 (Reimprimir) — a função mais usada, hoje quebrada.
2. **US2**: Alterar OS.
3. **US3 + US4**: Localização rápida e Clientes (podem ir juntas).
4. Publicar API + web juntos (as rotas antigas saem).
