# Contract: Endpoints HTTP da tela Home

> **Nota (feature 007):** as rotas GET citadas aqui foram substituídas pelo padrão REST: filas em `GET /service-orders/{fila}?pageNumber=&pageSize=` e busca por número em `GET /service-orders/{id}`. Ver `specs/007-rest-get-routes/contracts/http-endpoints.md`.

Todas as rotas: `.WithTags("ServiceOrder").RequireAuthorization()`. A resposta é sempre
`BaseResponse<T>` convertida com `.ToResult()` (o status HTTP vem de `BaseResponse.Code`).

## Filas (GET, paginadas)

Formato: `GET /get-<fila>-service-orders/{pageNumber}/{pageSize}` →
`BaseResponse<PaginatedResult<T>>`

| Rota nova | Rota antiga (removida) | Método do handler (`IServiceOrderQueryHandler`) | Método do repositório | T |
|-----------|------------------------|-------------------------------------------------|-----------------------|---|
| `/get-pending-estimate-service-orders` | `/get-pending-estimates-service-orders` | `GetPendingEstimateServiceOrders` | `GetPendingEstimateAsync` | `PendingEstimateServiceOrder` |
| `/get-awaiting-response-service-orders` | `/get-waiting-response-service-orders` | `GetAwaitingResponseServiceOrders` | `GetAwaitingResponseAsync` | `AwaitingResponseServiceOrder` |
| `/get-approved-service-orders` | `/get-pending-purchase-service-orders` | `GetApprovedServiceOrders` | `GetApprovedAsync` | `ApprovedServiceOrder` |
| `/get-rejected-service-orders` | `/get-rejected-estimate-service-orders` | `GetRejectedServiceOrders` | `GetRejectedAsync` | `RejectedServiceOrder` |
| `/get-awaiting-part-service-orders` | `/get-waiting-parts-service-orders` | `GetAwaitingPartServiceOrders` | `GetAwaitingPartAsync` | `AwaitingPartServiceOrder` |
| `/get-ready-for-pickup-service-orders` | `/get-waiting-pickup-service-orders` | `GetReadyForPickupServiceOrders` | `GetReadyForPickupAsync` | `ReadyForPickupServiceOrder` |

Formato do endpoint (igual para as 6 rotas):

```csharp
app.MapGet("/get-pending-estimate-service-orders/{pageNumber}/{pageSize}",
    async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
{
    var result = await handler.GetPendingEstimateServiceOrders(pageNumber, pageSize);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

Os campos de cada `T` estão em [data-model.md](../data-model.md).

## Orçamento (PUT)

`PUT /add-service-order-estimate` (rota mantida)

- **Body**: `AddServiceOrderEstimateCommand`
  ```json
  {
    "serviceOrderId": 123,
    "warranty": "90 DIAS",
    "solution": "TROCA DA PLACA",
    "totalCost": 350.00,
    "repairResult": 1,
    "estimateMessage": "...",
    "partPurchaseCost": 120.00
  }
  ```
  (antes: `guarantee` → agora: `warranty`)
- **Handler**: `IServiceOrderCommandHandler.AddEstimate(AddServiceOrderEstimateCommand)` →
  `BaseResponse<ServiceOrderDto?>` (sem MediatR)
- **Respostas**: 200 "Orçamento adicionado com sucesso!", 404 "Essa ordem de serviço não
  existe", erros de validação como hoje.
- **Efeito colateral no endpoint**: se a resposta for de sucesso,
  `hubContext.Clients.All.SendAsync("Atualizar", result.Message)`.

```csharp
app.MapPut("/add-service-order-estimate",
    async (IServiceOrderCommandHandler handler,
           AddServiceOrderEstimateCommand command,
           IHubContext<ServiceOrderHub> hubContext) =>
{
    var result = await handler.AddEstimate(command);
    if (result.IsSuccess)
        await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Angular

| Antes | Depois |
|-------|--------|
| `HomeService.getWaitingResponseServiceOrders` | `getAwaitingResponseServiceOrders` |
| `HomeService.getRefusedServiceOrders` | `getRejectedServiceOrders` |
| `HomeService.getWaitingPartServiceOrders` | `getAwaitingPartServiceOrders` |
| `HomeService.getReadyServiceOrders` | `getReadyForPickupServiceOrders` |
| `AddServiceOrderQuoteModalService.addServiceOrderQuote` | `AddServiceOrderEstimateModalService.addServiceOrderEstimate` |

`getPendingEstimateServiceOrders` e `getApprovedServiceOrders` mantêm o nome e passam a usar as
rotas novas.
