# Data Model: Modal "Liberado"

Nenhuma mudança de entidade, de schema ou de migration.

## ServiceOrder (existente, sem alteração)

| Método | Pré-condição (lança `EntityException<ServiceOrder>`) | Efeito |
|--------|------------------------------------------------------|--------|
| `MarkEstimateRejectedAsReadyToPickUp()` | `RepairStatus == Disapproved`, senão "O orçamento precisa estar rejeitado para marcar como pronto para entrega" | `ServiceOrderStatus = ReadyToPickUp` (o `RepairStatus` continua `Disapproved`) |

## Transição observada pelas abas

```text
Reprovados (Evaluated / Disapproved, resposta < 1 mês)
   └─ Liberado ─▶ Prontos (ReadyToPickUp / Disapproved)
```

## Efeito colateral

| Quando | WhatsApp |
|--------|----------|
| Sucesso, qualquer empresa | `SendRejectionReadyForPickupMessageRequest(CustomerName, PhoneNumber)` |
| Erro (0, não encontrada, não reprovada, falha ao salvar) | nenhum |

## Frontend

Usa o `ServiceOrder` compartilhado. Campos exibidos: `name`, `phone`, `phone2`, `type`, `brand`,
`model`, `serialNumber`, `accessories`, `defect`, `solution`, `totalAmount`, `guarantee`,
`responseDate`.
