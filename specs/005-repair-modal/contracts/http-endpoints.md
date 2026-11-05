# Contract: Registro de conserto

> **Nota (feature 007):** as rotas GET citadas aqui foram substituídas pelo padrão REST: filas em `GET /service-orders/{fila}?pageNumber=&pageSize=` e busca por número em `GET /service-orders/{id}`. Ver `specs/007-rest-get-routes/contracts/http-endpoints.md`.

## Registrar conserto

`PUT /service-orders/{id}/repair` (sem corpo). `.WithTags("ServiceOrder").RequireAuthorization()`.

| HTTP | `message` | Quando |
|------|-----------|--------|
| 200 | "Conserto adicionado com sucesso" | registrado |
| 404 | "Essa ordem de serviço não existe" | OS inexistente |
| 400 | "Não podemos executar o conserto pois a ordem de serviço não tem o status de aprovado" | orçamento pendente ou aguardando resposta (via `ExceptionMiddleware`) |

Efeito no endpoint: se der certo, `hubContext.Clients.All.SendAsync("Atualizar", result.Message)`.

```csharp
app.MapPut("/service-orders/{id:long}/repair",
    async (IServiceOrderCommandHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) =>
{
    var result = await handler.RegisterRepair(id);
    if (result.IsSuccess)
        await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Removido

- `PUT /add-service-order-repair` e `AddServiceOrderRepairCommand`

## Angular — `RepairModalService`

| Método | Chamada |
|--------|---------|
| `getServiceOrderById(id)` | `GET /get-service-order-by-id/{id}` |
| `registerRepair(id)` | `PUT /service-orders/{id}/repair` com body `{}` |
