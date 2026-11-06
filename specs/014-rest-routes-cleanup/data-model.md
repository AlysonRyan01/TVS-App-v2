# Data Model: Limpeza das rotas

**Sem mudança de schema.** Pagamentos e notificações continuam nas suas tabelas.

## Mapa final de rotas (37)

### ServiceOrders — consultas (`ServiceOrderQueryEndpoints`)
`GET /service-orders/pending-estimate` · `/awaiting-response` · `/approved` · `/rejected` ·
`/awaiting-part` · `/ready-for-pickup` · `GET /service-orders/search` · `GET /service-orders/{id}` ·
`GET /service-orders/{id}/pdf` · `GET /service-orders/{id}/payments`

### ServiceOrders — comandos (`ServiceOrderCommandEndpoints`)
`POST /service-orders` · `POST /service-orders/sales` · `PUT /service-orders/{id}` ·
`PUT /service-orders/{id}/estimate` · `/estimate/approve` · `/estimate/reject` · `/ready-for-pickup` ·
`/part/purchase` · `/part/purchase-cost` · `/repair` · `/delivery` · `/product/location` ·
`POST /service-orders/{id}/payments` · `PUT /service-orders/{id}/payments/{paymentId}` ·
`DELETE /service-orders/{id}/payments/{paymentId}`

### Customers
Consultas: `GET /customers?name=` · `GET /addresses/{cep}` — Comandos: `POST /customers` · `PUT /customers/{id}`

### Auth — comandos
`POST /auth/login` · `/auth/logout` · `/auth/refresh` · `/auth/register` (logado)

### Metrics — consultas
`GET /metrics`

### Public (site, sem login, caminhos mantidos)
Consultas: `GET /get-service-order-for-customer/{id}/{code}` —
Comandos: `PUT /add-service-order-approve-estimate` · `PUT /add-service-order-reject-estimate` ·
`POST /v1/whatsapp/site-contact`

### Fora dos grupos
`GET /` (saúde) · hub `/osHub` (SignalR)

## Regras novas (handlers)

| Handler | Regra |
|---------|-------|
| `GetForCustomer(id, code)` | código com trim/maiúsculas; OS inexistente ou código errado → 404 "Essa ordem de serviço não existe"; ok → `EstimateServiceOrder` |
| `ApproveEstimateByCustomer(id, code)` / `RejectEstimateByCustomer` | mesma conferência; ok → `ApproveEstimate(id)` / `RejectEstimate(id)` |
| `AddPayment` / `UpdatePayment` / `RemovePayment` / `GetPayments` | iguais aos handlers antigos (ver research R5) |
