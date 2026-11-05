# Data Model: Modal "Consertado"

Nenhuma mudança de entidade, de schema ou de migration.

## ServiceOrder (existente, sem alteração)

| Método | Pré-condição (lança `EntityException<ServiceOrder>`) | Efeito |
|--------|------------------------------------------------------|--------|
| `ExecuteRepair()` | `RepairStatus` diferente de `Entered` e `Waiting`, senão "Não podemos executar o conserto pois a ordem de serviço não tem o status de aprovado" | `ServiceOrderStatus = Repaired`, `RepairDate = DateTime.UtcNow` |

## Transição observada pelas abas

```text
Aguardando peça (OrderPart / Approved, compra < 3 meses)
   └─ Consertado ─▶ Prontos (Repaired, RepairDate = agora)
```

## Efeito colateral

| Quando | WhatsApp |
|--------|----------|
| Sucesso, qualquer empresa | `SendDeviceReadyMessageRequest(Id, CustomerName, PhoneNumber)` |
| Erro (não encontrada, orçamento não aprovado) | nenhum |

## Frontend

Usa o `ServiceOrder` compartilhado. Campos exibidos: `name`, `phone`, `phone2`, `type`, `brand`,
`model`, `serialNumber`, `accessories`, `defect`, `solution`, `totalAmount`, `guarantee`,
`purchasePartDate`.
