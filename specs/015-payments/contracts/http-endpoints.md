# HTTP Contracts: Pagamentos

Já existentes (feature 014), agora com validação:
- `GET /service-orders/{id}/payments` → `PaymentDto[]` (id, amount, paidAt)
- `POST /service-orders/{id}/payments` `{ amount, paymentDate }` → 200 "Pagamento adicionado." / 400 / 404
- `PUT /service-orders/{id}/payments/{paymentId}` `{ amount, paymentDate }` → 200 "Pagamento atualizado." / 400 / 404
- `DELETE /service-orders/{id}/payments/{paymentId}` → 200 "Pagamento removido." / 404

Novo:
- `GET /service-orders/{id}/payments/{paymentId}/receipt` → 200 `application/pdf`
  (`comprovante_pagamento_{paymentId}.pdf`); 404 OS ou pagamento inexistente (JSON).
