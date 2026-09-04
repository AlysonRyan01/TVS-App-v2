# Plano — Seção "Produtos Entregues" + Custo de Compra da Peça

## Objetivo

1. No lugar dos cards removidos (Reparos/Vendas por Tipo de Produto, Reparos/Vendas por
   Empresa, seção "Aparelhos Não Retirados"), criar no `/metricas` uma nova seção/tabela
   listando os **produtos entregues** (reparos + vendas) dentro do período selecionado
   (mês/ano ou intervalo de datas), com uma coluna de **status** identificando se a linha é
   Reparo ou Venda.
2. Essa mesma tabela terá, por linha, um **input editável** para o usuário preencher quanto
   a loja pagou pela peça daquela OS (`PartPurchaseCost` — novo campo em `ServiceOrder.cs`,
   distinto do `PartCost` já existente, que é o valor cobrado do cliente).
3. Com isso, calcular e exibir um novo KPI de "quanto estamos gastando" no período
   selecionado, somando `PartPurchaseCost` das ordens entregues nesse período.

## Decisões já confirmadas com o usuário

- **Escopo da lista**: reparos + vendas juntos, com coluna de status (Reparo/Venda).
- **Preenchimento do `PartPurchaseCost`**: acontece **somente** nessa nova grid de métricas
  (inline, por linha) — **não** faz parte do fluxo de "Comprado"
  (`AddPurchasePartDialog`/`AddPurchasedPartButton`/`AddPurchasedPartCommand`), que
  permanece inalterado.
- **Base temporal do KPI de gasto**: `DeliveryDate` dentro do período selecionado — ou seja,
  a mesma lista de `deliveredOrders` já usada para popular a grid alimenta também o somatório.

## Contexto já levantado no código

- `ServiceOrder.cs` já tem `PartCost` (valor cobrado do cliente, usado em `TotalAmount`) —
  não deve ser reaproveitado, é receita, não custo.
- `MetricsService.cs` já monta `deliveredOrders` (filtrado por `DeliveryDate` no período)
  tanto em `GetDashboardMetricsAsync` (mês/ano) quanto em `GetDashboardMetricsByRangeAsync`
  (intervalo). A nova grid e o novo KPI reaproveitam exatamente essa lista.
- `DashboardMetricsDto` é o contrato único devolvido pelos dois endpoints de métricas
  (`get-dashboard-metrics` e `get-dashboard-metrics-by-range`).
- Diferenciação Reparo x Venda hoje é feita por `Product.Defect == "VENDA"` (ver
  `MetricsService.BuildMetrics`).

## Parte 1 — Domain: novo campo `PartPurchaseCost`

- Criar Value Object `PartPurchaseCost` em `Src/TVSApp.Domain/ValueObjects/ServiceOrder/`,
  no mesmo molde de `PartCost.cs` (valida `>= 0`).
- `ServiceOrder.cs`: adicionar propriedade
  `PartPurchaseCost PartPurchaseCost { get; private set; } = new(0m);`
- Adicionar `EditPartPurchaseCost(PartPurchaseCost cost) => PartPurchaseCost = cost;`
  (paralelo a `EditPartCost`) — é o único ponto de entrada para esse valor, já que ele é
  preenchido depois, via a grid de métricas.
- **Não mexer** em `AddPurchasedPart()` — esse fluxo continua sem relação com o novo campo.

## Parte 2 — Infraestrutura

- **`ServiceOrderMapping.cs`**: `builder.OwnsOne(x => x.PartPurchaseCost, ...)` mapeando para
  coluna `PartPurchaseCost` (`HasPrecision(18,2)`), mesmo padrão de `PartCost`/`LaborCost`.
- **Migration EF**:
  `dotnet ef migrations add AddPartPurchaseCost --project Src/TVSApp.Infrastructure --startup-project Src/TVSApp.Api`

## Parte 3 — Application: comando para salvar o valor a partir da grid

- **Novo request** `UpdatePartPurchaseCostCommand(long ServiceOrderId, decimal PartPurchaseCost)`
  em `Src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/` — retorna
  `BaseResponse<bool>` (ou `ServiceOrderDto?`, a definir na implementação).
- **Novo handler** `UpdatePartPurchaseCostHandler`: busca a OS por id via
  `IServiceOrderQueryRepository`, chama `EditPartPurchaseCost`, persiste via
  `IServiceOrderCommandRepository.UpdateAsync`.
- **`ServiceOrderDto`**: adicionar `decimal PartPurchaseCost` (para manter o mapeamento
  completo da entidade) e incluir em `ServiceOrderMapper.MapToServiceOrderDto`.

## Parte 4 — API

- Novo endpoint em `ServiceOrderEndpoints.cs`, ex.:
  `PUT /service-orders/{id}/part-purchase-cost` (ou `PATCH`), recebendo o valor no corpo e
  disparando `UpdatePartPurchaseCostCommand` via MediatR — seguindo o padrão dos endpoints
  vizinhos (`AddPurchasedPartCommand` etc. em `ServiceOrderEndpoints.cs:502`).

## Parte 5 — Métricas (backend)

- **Novo DTO** `DeliveredProductDto` (em `Src/TVSApp.Application/DTOs/Dashboard/`):
  `Id`, `CustomerName`, `Brand`, `Model`, `Type` (label), `StatusLabel` ("Reparo"/"Venda"),
  `Enterprise` (label), `DeliveryDate`, `TotalAmount`, `PartPurchaseCost`.
- **`DashboardMetricsDto`**: adicionar `List<DeliveredProductDto> DeliveredProducts` e
  `decimal TotalPartsSpend`.
- **`MetricsService.BuildMetrics`**: a partir do `deliveredOrders` já carregado (reparos +
  vendas, sem filtrar por `Product.Defect`), projetar a lista `DeliveredProducts` ordenada
  por `DeliveryDate` desc, e calcular
  `TotalPartsSpend = deliveredOrders.Sum(x => x.PartPurchaseCost.ServiceOrderPartPurchaseCost)`.

## Parte 6 — Frontend

### Dashboard (`DashboardPage.razor` / `.razor.cs`)

- No lugar das duas `MudGrid` removidas (Linha 2/3), nova seção "Produtos Entregues" com
  `MudTable` sobre `_metrics.DeliveredProducts`: colunas Ordem, Cliente, Produto
  (marca/modelo), Tipo, **Status** (chip Reparo/Venda), Empresa, Data de Entrega, Valor
  Total, e uma coluna **"Valor da Peça"** com `MudNumericField` editável por linha.
- Novo KPI card "Gasto com Peças" = `FormatCurrency(_metrics.TotalPartsSpend)`, junto aos
  cards existentes.
- Ao editar o campo de valor da peça (on blur / Enter), chamar
  `DashboardHandler.UpdatePartPurchaseCostAsync(id, valor)`; em caso de sucesso, atualizar o
  item local e recalcular `TotalPartsSpend` no client (ou simplesmente disparar `LoadAsync`
  de novo para os dados virem já recalculados do backend — mais simples, a definir na hora).

### `DashboardHandler.cs`

- Novo método `UpdatePartPurchaseCostAsync(long serviceOrderId, decimal partPurchaseCost)`
  chamando o novo endpoint (`PUT/PATCH .../part-purchase-cost`).

## Ordem sugerida de implementação

1. Domain + Infra (Value Object, propriedade, mapping, migration).
2. Application + API (comando, handler, DTO, endpoint de update).
3. Backend de métricas (`DeliveredProductDto`, `DeliveredProducts`, `TotalPartsSpend` em
   `MetricsService`).
4. Frontend do dashboard (nova seção "Produtos Entregues" com input editável + card "Gasto
   com Peças").

## Pontos a decidir na implementação (não bloqueantes, mas vale alinhar)

- Após salvar o valor da peça pela grid, atualizar o `TotalPartsSpend` localmente no client
  ou simplesmente recarregar as métricas (`LoadAsync`/`LoadByRangeAsync`) para pegar o valor
  já recalculado do backend? (Recomendo recarregar — mais simples e sem risco de
  divergência.)
- O input da grid deve ficar sempre editável, ou só quando o valor ainda está zerado (`0`),
  travando depois de preenchido uma vez? O pedido não deixou isso explícito; por padrão vou
  deixar sempre editável (permite correção).
