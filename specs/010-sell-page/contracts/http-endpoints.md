# HTTP Contracts: Vender

## POST /service-orders/sales  *(novo; autenticado)*

Substitui `POST /create-sales-service-order` (removido).

**Request** (`application/json`):

```json
{
  "customerId": 123,
  "productType": 1,
  "productBrand": "samsung",
  "productModel": "un50au7700",
  "productSerialNumber": "abc123",
  "guarantee": "90 dias",
  "amount": 1499.90
}
```

Validações e mensagens: ver [data-model.md](../data-model.md) (400 pelo `ExceptionMiddleware`).

**Responses**:

| Status | Corpo | Quando |
|--------|-------|--------|
| 200 | `application/pdf` (comprovante de venda), cabeçalho `X-Service-Order-Id: {número}`, arquivo `venda_{número}.pdf` | venda registrada |
| 400 | `BaseResponse` JSON | validação |
| 500 | `BaseResponse` JSON | falha ao criar a OS, ou PDF falhou (OS **já** criada) |

**Efeitos** (só no 200): SignalR `Atualizar`; WhatsApp `SendProductSaleMessageRequest(nome,
telefone, valor, garantia, tipo)` enfileirado.
