# Data Model: Modal "Entregue"

Nenhuma mudança de entidade, de schema ou de migration.

## ServiceOrder (existente, sem alteração)

| Método | Pré-condição | Efeito |
|--------|--------------|--------|
| `AddDelivery()` | nenhuma | `ServiceOrderStatus = Delivered`, `DeliveryDate = DateTime.UtcNow` |

## Regra "OS consertada" (Application, igual ao antigo)

`RepairStatus == ERepairStatus.Approved && RepairResult == ERepairResult.Repair`

| Caso em "Prontos" | Consertada? | PDF | WhatsApp |
|-------------------|-------------|-----|----------|
| Consertada (aprovada, "Tem conserto", Repaired) | sim | termo de entrega | `SendGuaranteeMessageRequest(Id, Nome, Garantia, Telefone)` |
| Reprovada liberada (Disapproved) | não | — | — |
| Sem conserto / sem defeito (Approved + Unrepaired/NoDefectFound) | não | — | — |

## Frontend

- `ServiceOrder.repairStatus`: tipo corrigido de `ERepairResult` para `ERepairStatus` (R4).
- Campos exibidos: `name`, `phone`, `phone2`, `type`, `brand`, `model`, `serialNumber`,
  `accessories`, `defect`, `repairResult`, `solution`, `totalAmount`, `guarantee`, `repairDate`.
