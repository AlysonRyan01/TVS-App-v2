# Research: Pagamentos da OS + comprovante

Base: `TVS-App/Src/TVSApp.Web/Components/PaymentComponent.razor` (comportamento a reproduzir), as
rotas de pagamento da feature 014, a casca dos modais da Home (`service-order-modal` +
`service-order-ticket`), o painel da Buscar OS e os geradores QuestPDF existentes.

## R1. Onde a caixa aparece

- **Decision**: um componente `app-payments-panel` (input `serviceOrderId`, `totalAmount`) em
  `web/src/app/shared/payments/`, colocado:
  - no fim da **etiqueta** da OS (`service-order-ticket`), que todos os 6 modais da Home já usam →
    um lugar cobre os 6;
  - no painel da **Buscar OS** (`service-order-details-dialog`), como uma seção a mais.
- **Rationale**: mesma caixa nos 7 lugares (SC-002) com uma implementação só; a etiqueta já é a
  coluna de "informações da OS" de cada modal, então não tira espaço do formulário (Edge Case).
- **Alternatives**: slot `[modalBody]` em cada modal (6 edições repetidas).

## R2. Formulário de adicionar/editar

- **Decision**: formulário em linha dentro da própria caixa (valor `type="number"` com R$, data
  `type="date"`), em vez de um diálogo por cima do modal (o antigo abria diálogos). Editar troca a
  linha pelo formulário; remover pede confirmação na própria linha ("Remover este pagamento?
  Sim · Não").
- **Rationale**: evita diálogo sobre diálogo (modais da Home já são tela cheia).

## R3. Validação no servidor

- **Decision**: `AddPaymentCommand`/`UpdatePaymentCommand` ganham `Validate()` — valor > 0
  ("O valor deve ser maior que zero", mesma frase do antigo) e data obrigatória ("Informe a data do
  pagamento.") — via `CommandException` (400). Chamado no início de `AddPayment`/`UpdatePayment`.

## R4. Comprovante

- **Decision**: `IGenerateServiceOrderPdf.GeneratePaymentReceiptAsync(ServiceOrder, Payment)` e o
  documento `GeneratePaymentReceiptDocument` (QuestPDF, mesmo cabeçalho/cores do
  `GenerateSaleDocument`). Handler `GetPaymentReceipt(id, paymentId)` no
  `ServiceOrderCommandHandler` (já tem o gerador): OS (404) → pagamento (404 "Pagamento não
  encontrado") → PDF. Rota `GET /service-orders/{id}/payments/{paymentId}/receipt` →
  `application/pdf` (`comprovante_pagamento_{paymentId}.pdf`), na consulta de OS.
- **Resumo do comprovante**: calculado por `PaymentReceiptSummary.For(serviceOrder, payment)` na
  Application (testável): valor da OS (`TotalAmount`), total pago **até este pagamento** (pagamentos
  com data anterior, ou mesma data e Id ≤ o deste), saldo = valor − total pago (negativo = crédito).
- **Valor por extenso**: `ValorPorExtenso.Format(decimal)` na Application (reais e centavos,
  pt-BR: "cento e cinquenta reais e trinta centavos"), testado; até 999.999.999,99.
- **Frontend**: `openPdfWindow()` no clique + `showPdf` (padrão das outras telas).

## R5. Totais na tela

- Total pago = soma da lista; Falta = `totalAmount − totalPago`; 0 → "Quitado"; negativo →
  "Crédito de R$ X". Lista ordenada por data desc (e Id desc).
