# Data Model: Rotas GET no padrão REST

Nenhuma mudança de entidade, de schema, de DTO ou de formato de resposta.

## Parâmetros

| Parâmetro | Onde | Tipo | Padrão | Inválido |
|-----------|------|------|--------|----------|
| `{fila}` | caminho (literal) | uma de 6 | — | rota inexistente (404) |
| `{id}` | caminho | `long` | — | não numérico → não casa a rota (404) |
| `pageNumber` | query | `int` | 1 | não numérico → 400; `< 1` → 400 "O número da página deve ser maior que zero." (repositório, igual a hoje) |
| `pageSize` | query | `int` | 25 | não numérico → 400; `< 1` → 400 "O tamanho da página deve ser maior que zero." (repositório, igual a hoje) |

## Mapeamento fila → handler (inalterado)

| `{fila}` | Método do `IServiceOrderQueryHandler` | Tipo de item |
|----------|---------------------------------------|--------------|
| `pending-estimate` | `GetPendingEstimateServiceOrders` | `PendingEstimateServiceOrder` |
| `awaiting-response` | `GetAwaitingResponseServiceOrders` | `AwaitingResponseServiceOrder` |
| `approved` | `GetApprovedServiceOrders` | `ApprovedServiceOrder` |
| `rejected` | `GetRejectedServiceOrders` | `RejectedServiceOrder` |
| `awaiting-part` | `GetAwaitingPartServiceOrders` | `AwaitingPartServiceOrder` |
| `ready-for-pickup` | `GetReadyForPickupServiceOrders` | `ReadyForPickupServiceOrder` |
