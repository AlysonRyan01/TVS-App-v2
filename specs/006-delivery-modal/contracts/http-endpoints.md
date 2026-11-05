# Contract: Entrega da OS

> **Nota (feature 007):** as rotas GET citadas aqui foram substituídas pelo padrão REST: filas em `GET /service-orders/{fila}?pageNumber=&pageSize=` e busca por número em `GET /service-orders/{id}`. Ver `specs/007-rest-get-routes/contracts/http-endpoints.md`.

## Registrar entrega

`PUT /service-orders/{id}/delivery` (sem corpo). `.WithTags("ServiceOrder").RequireAuthorization()`.

| HTTP | Content-Type | Corpo | Quando |
|------|--------------|-------|--------|
| 200 | `application/pdf` | termo de entrega (`ordem_servico_{id}.pdf`) | OS consertada |
| 200 | `application/json` | `BaseResponse` com "Ordem de serviço entregue com sucesso!" | OS não consertada |
| 404 | `application/json` | "Essa ordem de serviço não existe" | OS inexistente |
| 500 | `application/json` | mensagem do gerador de PDF | falha ao gerar o PDF (a entrega fica salva) |

Efeito no endpoint: se der certo, `hubContext.Clients.All.SendAsync("Atualizar", result.Message)`.

```csharp
app.MapPut("/service-orders/{id:long}/delivery",
    async (IServiceOrderCommandHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) =>
{
    var result = await handler.RegisterDelivery(id);
    if (!result.IsSuccess)
        return result.ToResult();

    await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

    if (result.Data != null)
        return Results.File(result.Data, "application/pdf", $"ordem_servico_{id}.pdf");

    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Removido

- `PUT /add-service-order-delivery` e `AddServiceOrderDeliveryAndReturnPdfCommand`

## Angular — `DeliveryModalService`

| Método | Chamada |
|--------|---------|
| `getServiceOrderById(id)` | `GET /get-service-order-by-id/{id}` |
| `registerDelivery(id)` | `PUT /service-orders/{id}/delivery` com body `{}`, `observe: 'response'`, `responseType: 'blob'` → `Observable<HttpResponse<Blob>>` |
