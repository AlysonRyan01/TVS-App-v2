# Data Model: Pagamentos

**Sem mudança de schema.** `Payment` (Id, Amount, PaidAt, ServiceOrderId) já existe.

## Validação (Application)

| Command | Regra | Erro (400) |
|---------|-------|-----------|
| AddPaymentCommand / UpdatePaymentCommand | `Amount > 0` | "O valor deve ser maior que zero" |
| idem | `PaymentDate != default` | "Informe a data do pagamento." |

## PaymentReceiptSummary (Application, novo)

`record PaymentReceiptSummary(decimal ServiceOrderTotal, decimal PaidUntilThisPayment, decimal Balance)`
— `For(ServiceOrder order, Payment payment)`: soma os pagamentos com `PaidAt < payment.PaidAt` ou
(`PaidAt == payment.PaidAt` e `Id <= payment.Id`); `Balance = ServiceOrderTotal − PaidUntilThisPayment`.

## ValorPorExtenso (Application, novo)

`static string Format(decimal value)` — ex.: 1 → "um real"; 150,30 → "cento e cinquenta reais e
trinta centavos"; 0,50 → "cinquenta centavos"; 1.000.000 → "um milhão de reais"; 2.500 → "dois mil
e quinhentos reais".

## Comprovante (PDF)

Cabeçalho da loja · "COMPROVANTE DE PAGAMENTO Nº {paymentId}" · OS {id} · cliente (nome, telefone,
documento) · aparelho (tipo, marca, modelo, série) · valor em destaque + por extenso · data do
pagamento · resumo (valor da OS, total pago até este pagamento, saldo/crédito) · data de emissão.
