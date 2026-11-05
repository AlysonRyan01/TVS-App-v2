# Data Model: Modal "Comprado"

Nenhuma mudança de entidade, de schema ou de migration.

## ServiceOrder (existente, sem alteração)

| Método | Efeito |
|--------|--------|
| `AddPurchasedPart()` | `ServiceOrderStatus = EServiceOrderStatus.OrderPart`; `PurchasePartDate = DateTime.UtcNow` |

Sem pré-condições (igual ao handler antigo).

## Transição observada pelas abas

```text
Aprovados (Evaluated / Approved / sem PurchasePartDate)
   └─ Comprado ─▶ Aguardando peça (OrderPart / PurchasePartDate = agora)
```

## Frontend

Usa o `ServiceOrder` compartilhado. Campos exibidos: `name`, `phone`, `phone2`, `type`, `brand`,
`model`, `serialNumber`, `accessories`, `defect`, `solution`, `totalAmount`, `partPurchaseCost`,
`guarantee`, `responseDate`.
