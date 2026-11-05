# Contract: Resposta do orçamento

> **Nota (feature 007):** as rotas GET citadas aqui foram substituídas pelo padrão REST: filas em `GET /service-orders/{fila}?pageNumber=&pageSize=` e busca por número em `GET /service-orders/{id}`. Ver `specs/007-rest-get-routes/contracts/http-endpoints.md`.

Todas as rotas: `.WithTags("ServiceOrder").RequireAuthorization()`. A resposta é
`BaseResponse<ServiceOrderDto?>` convertida com `.ToResult()`.

## Aprovar

`PUT /service-orders/{id}/estimate/approve` (sem corpo)

| HTTP | `message` | Quando |
|------|-----------|--------|
| 200 | "Aprovação adicionada com sucesso!" | aprovado |
| 404 | "Essa ordem de serviço não existe" | OS inexistente |
| 409 | "Esse orçamento já foi respondido." | OS não está aguardando resposta |
| 400 | mensagem do domínio (ex.: "A solução não pode estar nula…") | via middleware (R6) |

## Reprovar

`PUT /service-orders/{id}/estimate/reject` (sem corpo)

Mesma tabela, com 200 → "Orçamento reprovado com sucesso!".

## Formato do endpoint

```csharp
app.MapPut("/service-orders/{id:long}/estimate/approve",
    async (IServiceOrderCommandHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) =>
{
    var result = await handler.ApproveEstimate(id);
    if (result.IsSuccess)
        await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
    return result.ToResult();
}).WithTags("ServiceOrder").RequireAuthorization();
```

## Rotas removidas

- `PUT /add-service-order-approve-estimate`
- `PUT /add-service-order-reject-estimate`

(e os records `AddServiceOrderApproveEstimateCommand` / `AddServiceOrderRejectEstimateCommand`)

## Angular

`EstimateResponseModalService`:

| Método | Chamada |
|--------|---------|
| `getServiceOrderById(id)` | `GET /get-service-order-by-id/{id}` |
| `approveEstimate(id)` | `PUT /service-orders/{id}/estimate/approve` com body `{}` |
| `rejectEstimate(id)` | `PUT /service-orders/{id}/estimate/reject` com body `{}` |

Todos retornam `Observable<BaseResponse<ServiceOrder>>`.

## Erros (todas as rotas)

Com a correção do R6, exceções de validação passam a responder com o status HTTP igual ao
`code` do corpo (400/500), em vez de 200.
