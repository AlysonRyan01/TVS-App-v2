# Contract: Liberação de OS reprovada

> **Nota (feature 007):** as rotas GET citadas aqui foram substituídas pelo padrão REST: filas em `GET /service-orders/{fila}?pageNumber=&pageSize=` e busca por número em `GET /service-orders/{id}`. Ver `specs/007-rest-get-routes/contracts/http-endpoints.md`.

## Liberar

`PUT /service-orders/{id}/ready-for-pickup` (sem corpo). `.WithTags("ServiceOrder").RequireAuthorization()`.

| HTTP | `message` | Quando |
|------|-----------|--------|
| 200 | "Ordem de serviço marcada como pronta para entrega" | liberada |
| 400 | "O número da ordem de serviço não pode ser 0" | `id == 0` |
| 404 | mensagem do repositório (ex.: "A ordem de serviço com ID:{id} não existe") | não encontrada / id inválido |
| 400 | "O orçamento precisa estar rejeitado para marcar como pronto para entrega" | orçamento não reprovado (via `ExceptionMiddleware`) |
| código do update | mensagem do update | falha ao salvar |

Efeito no endpoint: se der certo, `hubContext.Clients.All.SendAsync("Atualizar", result.Message)`.

```csharp
app.MapPut("/service-orders/{id:long}/ready-for-pickup",
    async (IServiceOrderCommandHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) =>
{
    var result = await handler.MarkRejectedEstimateAsReadyForPickup(id);
    if (result.IsSuccess)
        await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Removido

- `PUT /mark-estimate-rejected-as-ready-to-pick-up` e `MarkEstimateRejectedAsReadyToPickUpCommand`

## Angular — `RejectedReleaseModalService`

| Método | Chamada |
|--------|---------|
| `getServiceOrderById(id)` | `GET /get-service-order-by-id/{id}` |
| `markAsReadyForPickup(id)` | `PUT /service-orders/{id}/ready-for-pickup` com body `{}` |
