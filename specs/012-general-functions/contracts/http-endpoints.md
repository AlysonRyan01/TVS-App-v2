# HTTP Contracts: Funções gerais

## PUT /service-orders/{id}  *(novo; autenticado)* — Alterar OS

Corpo: campos de `EditServiceOrderCommand` ([data-model.md](../data-model.md)), datas ISO
(`yyyy-MM-dd`), enums numéricos.

| Status | Corpo |
|--------|-------|
| 200 | `BaseResponse<ServiceOrderDto>` com a OS atualizada, mensagem "Ordem de serviço alterada com sucesso!" |
| 400 | validação |
| 404 | "Essa ordem de serviço não existe" |

Efeito: SignalR `Atualizar`.

## GET /service-orders/{id}/pdf  *(novo; autenticado)* — Reimprimir

200 `application/pdf` (`ordem_servico_{id}.pdf`); 404 / 500 como `BaseResponse` JSON.

## PUT /service-orders/{id}/product/location  *(existente)* — Localização

Sem mudança.

## PUT /customers/{id}  *(existente)* — Editar cliente

Sem mudança.

## Removidas

`PUT /edit-service-order`, `POST /regenerate-service-order-pdf`.
