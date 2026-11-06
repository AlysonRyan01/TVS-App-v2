# Tasks: Pagamentos da OS + comprovante

**Input**: Design documents from `/specs/015-payments/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos — FR-007 e a constituição (IV).

**Organization**: US1 caixa de pagamentos nos 7 lugares, US2 comprovante, US3 validações e erros.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (163 verdes), `cd web && npx ng build`

---

## Phase 2: Foundational (validação no servidor)

- [X] T002 Testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs`: `AddPayment`/`UpdatePayment` com valor 0 ou negativo lançam `CommandException` "O valor deve ser maior que zero" e com data `default` lançam "Informe a data do pagamento.", sem gravar
- [X] T003 Adicionar `Validate()` a `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/AddPaymentCommand.cs` e `UpdatePaymentCommand.cs` (regras de data-model.md) e chamar no início de `AddPayment`/`UpdatePayment` em `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs`; T002 deve passar

---

## Phase 3: User Story 1 — Pagamentos da OS nos 7 lugares (P1) 🎯 MVP

**Goal**: caixa Pagamentos com lista, totais, adicionar, editar e remover.

**Independent Test**: quickstart §2 itens 1–4 e 7.

- [X] T004 [P] [US1] Criar `web/src/app/shared/payments/payments.service.ts` (`list(id)`, `add(id, { amount, paymentDate })`, `update(id, paymentId, …)`, `remove(id, paymentId)`, `receipt(id, paymentId): Observable<HttpResponse<Blob>>` em `/service-orders/{id}/payments…`) e o modelo `Payment { id, amount, paidAt }`
- [X] T005 [US1] Criar `web/src/app/shared/payments/payments-panel.component.{ts,html,scss}` (`app-payments-panel`, inputs `serviceOrderId` e `totalAmount`): título "Pagamentos" + botão "Adicionar"; lista mais recente primeiro (valor em R$, data dd/MM/yyyy, ações editar/remover/imprimir); formulário em linha (valor `type="number"` com R$, data `type="date"` padrão hoje, Cancelar/Salvar); remover com confirmação na linha ("Remover este pagamento?" Sim · Não); vazio → "Nenhum pagamento registrado."; rodapé com Total pago, Valor da OS e Falta / "Quitado" / "Crédito de R$ X"; snackbars "Pagamento adicionado/atualizado/removido." (mensagens da API); visual da etiqueta (fundo `$surface`, bordas `#ead8d3`); SCSS < 4 kB
- [X] T006 [US1] Colocar `<app-payments-panel [serviceOrderId]="serviceOrderId()" [totalAmount]="os.totalAmount" />` no fim de `web/src/app/features/home/modals/shared/service-order-ticket.component.html` (importar no `.ts`) — cobre os 6 modais da Home
- [X] T007 [US1] Adicionar a seção "Pagamentos" com `<app-payments-panel>` no painel `web/src/app/features/search-service-orders/service-order-details-dialog/service-order-details-dialog.component.html` (na coluna da direita, depois de Datas; `totalAmount` = total do formulário)

---

## Phase 4: User Story 2 — Comprovante de pagamento (P1)

**Goal**: botão Imprimir comprovante gera o PDF no servidor.

**Independent Test**: quickstart §2 item 6.

- [X] T008 [P] [US2] Testes em `src/TVSApp.Tests/Application/ValorPorExtensoTests.cs`: 1 → "um real"; 2 → "dois reais"; 0,50 → "cinquenta centavos"; 0,01 → "um centavo"; 150,30 → "cento e cinquenta reais e trinta centavos"; 100 → "cem reais"; 2.500 → "dois mil e quinhentos reais"; 1.234,56 → "mil duzentos e trinta e quatro reais e cinquenta e seis centavos"; 1.000.000 → "um milhão de reais"; 2.000.001 → "dois milhões e um reais"
- [X] T009 [US2] Criar `src/TVSApp.Application/Services/ValorPorExtenso.cs` (`public static string Format(decimal value)`, pt-BR, até 999.999.999,99); T008 deve passar
- [X] T010 [P] [US2] Testes em `src/TVSApp.Tests/Application/PaymentReceiptSummaryTests.cs`: com 3 pagamentos em datas diferentes, o resumo do 2º soma só 1º + 2º; mesma data → desempata por Id; `Balance = TotalAmount − pago até este`; pago a mais → saldo negativo
- [X] T011 [US2] Criar `src/TVSApp.Application/Services/PaymentReceiptSummary.cs` (`record PaymentReceiptSummary(decimal ServiceOrderTotal, decimal PaidUntilThisPayment, decimal Balance)` + `static For(ServiceOrder order, Payment payment)`); T010 deve passar
- [X] T012 [US2] Testes em `ServiceOrderCommandHandlerTests.cs` para `GetPaymentReceipt(id, paymentId)`: OS inexistente → 404 "Essa ordem de serviço não existe"; pagamento inexistente → 404 "Pagamento não encontrado"; falha do gerador → 500; sucesso → bytes do PDF (ampliar `FakeServiceOrderPdfGenerator` com `ReceiptResponse`/`ReceiptCalls`)
- [X] T013 [US2] Adicionar `Task<BaseResponse<byte[]>> GeneratePaymentReceiptAsync(ServiceOrder serviceOrder, Payment payment)` em `src/TVSApp.Application/Interfaces/Services/IGenerateServiceOrderPdf.cs`; implementar `GetPaymentReceipt` em `IServiceOrderCommandHandler.cs`/`ServiceOrderCommandHandler.cs`; T012 deve passar
- [X] T014 [US2] Criar `src/TVSApp.Infrastructure/Services/QuestPDFs/GeneratePaymentReceiptDocument.cs` (`IDocument`, mesmo cabeçalho/cores do `GenerateSaleDocument`): "COMPROVANTE DE PAGAMENTO Nº {paymentId}", OS, cliente (nome, telefone, documento formatado), aparelho (tipo, marca, modelo, série), valor em destaque + `ValorPorExtenso.Format`, data do pagamento, resumo (`PaymentReceiptSummary`: valor da OS, total pago até este, saldo ou "Crédito"), data de emissão; e `GeneratePaymentReceiptAsync` em `src/TVSApp.Infrastructure/Services/GenerateServiceOrderPdfService.cs`
- [X] T015 [US2] Rota `GET /service-orders/{id:long}/payments/{paymentId:long}/receipt` em `src/TVSApp.Api/Endpoints/ServiceOrders/ServiceOrderQueryEndpoints.cs` (erro → `ToResult()`; sucesso → `Results.File(pdf, "application/pdf", $"comprovante_pagamento_{paymentId}.pdf")`; `RequireAuthorization`)
- [X] T016 [US2] No `app-payments-panel`: botão **Imprimir comprovante** (ícone `receipt_long`) em cada linha → `openPdfWindow()` no clique, `receipt()`, `showPdf(body, win, 'comprovante_pagamento_{id}.pdf', 'Comprovante de pagamento {id}')`; erro → fecha a aba + snackbar com `readBlobErrorMessage`; spinner na linha

---

## Phase 5: User Story 3 — Validações e erros (P2)

**Goal**: campos em vermelho, Salvar bloqueado, formulário preservado em erro.

**Independent Test**: quickstart §2 item 5.

- [X] T017 [US3] No `app-payments-panel`: valor `Validators.required` + `Validators.min(0.01)`, data `Validators.required`; Salvar bloqueado se inválido ou enviando; erro da API → snackbar com a mensagem e o formulário continua aberto com os valores

---

## Phase 6: Polish & validação

- [X] T018 [P] `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` verdes
- [X] T019 [P] `cd web && npx ng build` sem erro (inicial < 1 MB; SCSS < 4 kB)
- [ ] T020 Roteiro manual do quickstart §2

---

## Dependencies & Execution Order

- T001 → T002 → T003 → US1 / US2 (backend).
- US1: T004 → T005 → T006 ∥ T007.
- US2: T008 → T009; T010 → T011; T012 → T013 → T014 → T015 (depois de T009/T011); T016 depois de T005 e T015.
- US3: T017 depois de T005.
- Polish por último.

### Parallel Opportunities

- T004 ∥ T008 ∥ T010 (arquivos diferentes); T006 ∥ T007; T018 ∥ T019.

### Parallel Example

```text
T004 payments.service.ts  |  T008 ValorPorExtensoTests  |  T010 PaymentReceiptSummaryTests
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 → pagamentos nos 7 lugares.
2. **US2**: comprovante (PDF + botão).
3. **US3**: validações na tela (pequeno; pode entrar junto com T005).
