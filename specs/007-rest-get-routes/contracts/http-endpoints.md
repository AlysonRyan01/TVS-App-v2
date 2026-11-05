# Contract: Leituras REST

Todas as rotas: `.WithTags("ServiceOrder").RequireAuthorization()`, resposta com `.ToResult()`
(a mesma `BaseResponse` de hoje).

## Filas (tabelas da Home)

| Rota nova | Rota removida |
|-----------|---------------|
| `GET /service-orders/pending-estimate?pageNumber=1&pageSize=25` | `GET /get-pending-estimate-service-orders/{pageNumber}/{pageSize}` |
| `GET /service-orders/awaiting-response?pageNumber=1&pageSize=25` | `GET /get-awaiting-response-service-orders/{pageNumber}/{pageSize}` |
| `GET /service-orders/approved?pageNumber=1&pageSize=25` | `GET /get-approved-service-orders/{pageNumber}/{pageSize}` |
| `GET /service-orders/rejected?pageNumber=1&pageSize=25` | `GET /get-rejected-service-orders/{pageNumber}/{pageSize}` |
| `GET /service-orders/awaiting-part?pageNumber=1&pageSize=25` | `GET /get-awaiting-part-service-orders/{pageNumber}/{pageSize}` |
| `GET /service-orders/ready-for-pickup?pageNumber=1&pageSize=25` | `GET /get-ready-for-pickup-service-orders/{pageNumber}/{pageSize}` |

```csharp
app.MapGet("/service-orders/pending-estimate",
    async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
{
    var result = await handler.GetPendingEstimateServiceOrders(pageNumber, pageSize);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## OS por número

| Rota nova | Rota removida |
|-----------|---------------|
| `GET /service-orders/{id:long}` | `GET /get-service-order-by-id/{id}` |

```csharp
app.MapGet("/service-orders/{id:long}",
    async (IServiceOrderQueryHandler handler, long id) =>
{
    var result = await handler.GetServiceOrderById(id);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Angular

| Arquivo | Antes | Depois |
|---------|-------|--------|
| `home.service.ts` (6 métodos) | `` `${apiUrl}/get-{fila}-service-orders/${pageNumber}/${pageSize}` `` | `` `${apiUrl}/service-orders/{fila}` `` + `params: { pageNumber, pageSize }` |
| 6 serviços de modal, `getServiceOrderById` | `` `${apiUrl}/get-service-order-by-id/${id}` `` | `` `${apiUrl}/service-orders/${id}` `` |
