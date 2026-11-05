# Data Model: Modal de resposta do orçamento

Nenhuma mudança de schema e nenhuma migration. As colunas usadas (`RepairStatus`,
`ResponseDate`) já existem.

## ServiceOrder (Domain, entidade existente)

### Novo membro

| Membro | Tipo | Regra |
|--------|------|-------|
| `IsAwaitingEstimateResponse` | `bool` (somente leitura) | `ServiceOrderStatus == Evaluated && RepairStatus == ERepairStatus.Waiting` |

### Métodos alterados

| Método | Pré-condições (lançam `EntityException<ServiceOrder>`) | Efeito |
|--------|--------------------------------------------------------|--------|
| `ApproveEstimate()` | solução preenchida (já existe); `IsAwaitingEstimateResponse` → senão "Esse orçamento já foi respondido." | `RepairStatus = Approved`, `ResponseDate = UtcNow` |
| `RejectEstimate()` | solução preenchida (já existe); `IsAwaitingEstimateResponse` → senão "Esse orçamento já foi respondido." | `RepairStatus = Disapproved`, `ResponseDate = UtcNow` |

### Transições

```text
Evaluated / Waiting ──ApproveEstimate()──▶ Evaluated / Approved     (aba Aprovados)
Evaluated / Waiting ──RejectEstimate()───▶ Evaluated / Disapproved  (aba Reprovados)
qualquer outro estado ──Approve/Reject──▶ EntityException "Esse orçamento já foi respondido."
```

## Efeitos colaterais (Application)

| Ação | Notificação interna (título / mensagem) | WhatsApp (igual a `old-handlers/`) |
|------|------------------------------------------|-------------------------------------------|
| Aprovar | "Orçamento Aprovado, OS: {id}" / "Favor comprar peça para OS: {id}" | Só se ≠ PhilcoBritania: `SendThankYouForEstimateApprovalRequest(id, nome, telefone, TotalAmount)` + `NotifyOwnerEstimateDecisionRequest(id, Approved, nome)` |
| Reprovar | "Orçamento Reprovado, OS: {id}" / "Favor montar a OS {id} para entrega" | Sempre: `SendEstimateDisapprovalAcknowledgmentRequest(id, nome, telefone)` + `NotifyOwnerEstimateDecisionRequest(id, Disapproved, nome)` |

## Frontend

O modal reaproveita `ServiceOrder` (`web/src/app/shared/models/service-order.interface.ts`), sem
modelo novo. Campos exibidos: `id`, `name`, `phone`, `phone2`, `brand`, `model`, `serialNumber`,
`accessories`, `defect`, `repairResult`, `solution`, `totalAmount`, `guarantee`,
`estimateMessage`.
