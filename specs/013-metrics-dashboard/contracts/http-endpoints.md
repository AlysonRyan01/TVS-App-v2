# HTTP Contracts: Métricas

## GET /metrics  *(novo; autenticado)*

- `?month=11&year=2026` (padrão: mês atual) **ou** `?from=2026-11-01&to=2026-11-15`.
- 200: `BaseResponse<DashboardMetricsDto>` ([data-model.md](../data-model.md)).
- 400: período inválido.

## PUT /service-orders/{id}/part/purchase-cost  *(novo; autenticado)*

Corpo `{ "partPurchaseCost": 120.50 }` → 200 `BaseResponse<ServiceOrderDto>` "Valor da peça
atualizado."; 400 negativo; 404 OS inexistente.

## Removidas

`GET /get-dashboard-metrics`, `GET /get-dashboard-metrics-by-range`,
`PUT /update-service-order-part-purchase-cost`. (`GET /get-abandoned-orders` continua.)
