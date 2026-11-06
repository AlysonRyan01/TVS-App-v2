# Data Model: Métricas

**Sem mudança de schema.**

## DashboardMetricsDto (existente; campos novos em **negrito**)

| Campo | Definição |
|-------|-----------|
| TotalOrders | OS de reparo (defeito ≠ "VENDA") com entrada no período |
| ActiveOrders | dessas, situação ≠ Entregue |
| TotalRevenue | soma `TotalAmount` das OS de reparo entregues no período, aprovadas e com conserto |
| ApprovalRate | aprovados ÷ (aprovados + reprovados) × 100, 1 casa; 0 sem orçamentos |
| AverageCycleDays | média (dias, 1 casa) de `DeliveryDate − EntryDate` das OS de reparo do período entregues, **só com DeliveryDate ≥ EntryDate** |
| TotalSales | OS de venda com entrada no período |
| TotalSalesRevenue | soma `TotalAmount` das vendas entregues no período |
| PendingDeliveryRevenue | soma `TotalAmount` das OS de reparo aprovadas, com conserto, não entregues |
| TotalPartsSpend | soma `PartPurchaseCost` dos entregues no período (aprovados) |
| **TotalValue** | TotalRevenue + TotalSalesRevenue |
| **NetTotal** | TotalValue − TotalPartsSpend |
| OrdersByStatus* | contagem por situação |
| RepairResult* | contagem por resultado |
| DeliveredProducts | entregues no período, mais recentes primeiro |

## DeliveredProductDto (existente; campo novo em **negrito**)

`Id, CustomerName, Brand, Model, **ProductLabel** ("MARCA MODELO" + " - ACESSÓRIOS" se houver),
TypeLabel, StatusLabel ("Reparo"/"Venda"), EnterpriseLabel, DeliveryDate, TotalAmount,
PartPurchaseCost`.

## Período (handler)

| Entrada | Regra | Erro (400) |
|---------|-------|------------|
| from + to | to ≥ from; dias inteiros | "A data final não pode ser antes da inicial." |
| só from ou só to | — | "Informe as duas datas do período." |
| month/year | 1–12 / 2000–2100; padrão: mês atual | "Mês ou ano inválido." |

## Valor da peça

`PUT /service-orders/{id}/part/purchase-cost` `{ partPurchaseCost: decimal }` → `EditPartPurchaseCost`;
negativo recusado pelo value object ("O valor de compra da peça não pode ser menor que 0").
