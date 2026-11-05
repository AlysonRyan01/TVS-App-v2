# Contract: Compra da peça

> **Nota (feature 007):** as rotas GET citadas aqui foram substituídas pelo padrão REST: filas em `GET /service-orders/{fila}?pageNumber=&pageSize=` e busca por número em `GET /service-orders/{id}`. Ver `specs/007-rest-get-routes/contracts/http-endpoints.md`.

## Registrar compra

`PUT /service-orders/{id}/part/purchase` (sem corpo). `.WithTags("ServiceOrder").RequireAuthorization()`.

| HTTP | `message` | Quando |
|------|-----------|--------|
| 200 | "Compra de peça adicionada com sucesso!" | registrado |
| 404 | "Essa ordem de serviço não existe" | OS inexistente |

Efeito no endpoint: se der certo, `hubContext.Clients.All.SendAsync("Atualizar", result.Message)`.

```csharp
app.MapPut("/service-orders/{id:long}/part/purchase",
    async (IServiceOrderCommandHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) =>
{
    var result = await handler.RegisterPartPurchase(id);
    if (result.IsSuccess)
        await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Removido

- `PUT /add-service-order-purchased-part` e `AddPurchasedPartCommand`

## Angular — `PartPurchaseModalService`

| Método | Chamada |
|--------|---------|
| `getServiceOrderById(id)` | `GET /get-service-order-by-id/{id}` |
| `registerPartPurchase(id)` | `PUT /service-orders/{id}/part/purchase` com body `{}` |
