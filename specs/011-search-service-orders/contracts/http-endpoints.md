# HTTP Contracts: Buscar OS

## GET /service-orders/search  *(novo; autenticado)*

Query string — **um** filtro por vez:

| Parâmetro | Exemplo |
|-----------|---------|
| `number` | `4312` |
| `customer` | `maria` |
| `model` | `42lb` |
| `serialNumber` | `sn12` |
| `from`, `to` | `2026-10-01`, `2026-10-31` (um ou os dois) |
| `pageNumber`, `pageSize` | `1`, `20` (padrão) |

**200**:

```json
{
  "data": {
    "items": [
      { "number": 4312, "customer": "MARIA SOUZA", "productType": 1, "brand": "LG",
        "model": "42LB5600", "serialNumber": "SN123", "enterprise": 1,
        "serviceOrderStatus": 4, "isSale": false, "enteredAt": "2026-11-03T13:10:00Z" }
    ],
    "totalCount": 48, "pageNumber": 1, "pageSize": 20, "totalPages": 3
  },
  "code": 200,
  "message": "48 ordens encontradas."
}
```

Sem resultado: 200 com `items: []`, `totalCount: 0`. Erros 400: ver
[data-model.md](../data-model.md).

## GET /service-orders/{id}  *(existente)*

Detalhes para o painel (`ServiceOrderDto`). Sem mudança.

## Removidas

`GET /get-service-orders-by-customer-name`, `/get-service-orders-by-serial-number`,
`/get-service-orders-by-model`, `/get-service-orders-by-date`.
