# Research: Tela "Métricas"

Base: `MetricsService` (Infrastructure, já calcula tudo por mês/ano e por intervalo),
`DashboardMetricsDto`, `GetDashboardMetrics*Handler` (MediatR, ainda registrados),
`old-handlers/UpdatePartPurchaseCostHandler.cs`, a rota quebrada
`update-service-order-part-purchase-cost` e a imagem do sistema antigo.

## R1. Onde fica o cálculo (testável)

- **Decision**: extrair o `BuildMetrics` (hoje `private static` no `MetricsService`) para
  `TVSApp.Application/Services/DashboardMetricsCalculator.cs` (`public static DashboardMetricsDto
  Build(IReadOnlyList<ServiceOrder> orders, IReadOnlyList<ServiceOrder> deliveredOrders)`). O
  `MetricsService` (Infrastructure) só consulta o banco e chama o calculador.
- **Rationale**: a constituição exige testes dos cálculos (FR-008); com o cálculo puro na
  Application, os testes montam OS com `ServiceOrderFactory` e métodos de domínio, sem banco.
- **Alternatives**: testar o `MetricsService` com banco em memória (o projeto de testes não tem EF).

## R2. Ajustes no cálculo

- **Tempo médio de ciclo**: só OS com `DeliveryDate >= EntryDate` entram na média (corrige o
  "-0,6 dias" da imagem).
- **Novos campos no DTO**: `TotalValue = TotalRevenue + TotalSalesRevenue` e
  `NetTotal = TotalValue - TotalPartsSpend` (os cartões "Valor total" e "Total líquido" da imagem;
  calculados no servidor para serem testados).
- **Entregues**: `DeliveredProductDto` ganha `ProductLabel` = "MARCA MODELO" + " - ACESSÓRIOS"
  quando houver (a imagem mostra, ex., "SAMSUNG UN58MU6120G - SEM ACESSÓRIOS"), montado no servidor.
- O resto das definições fica igual (spec US1-4 reproduz o serviço atual).

## R3. Rotas

- **Decision**: `IMetricsQueryHandler`/`MetricsQueryHandler` (sem MediatR) com
  `GetDashboard(int? month, int? year, DateTime? from, DateTime? to)`:
  - De e Até preenchidos → intervalo (Até ≥ De; senão 400 "A data final não pode ser antes da
    inicial."); só um dos dois → 400 "Informe as duas datas do período.";
  - senão Mês/Ano (padrão: mês atual; mês 1–12 e ano 2000–2100, senão 400 "Mês ou ano inválido.").
  Rota única `GET /metrics?month=&year=` ou `GET /metrics?from=&to=`.
- Remover `get-dashboard-metrics`, `get-dashboard-metrics-by-range`, as duas queries e os dois
  handlers MediatR. `get-abandoned-orders` fica (fora do escopo).

## R4. Valor da peça

- **Decision**: `UpdatePartPurchaseCost(long id, decimal partPurchaseCost)` em
  `ServiceOrderCommandHandler`, igual ao antigo: busca (404 "Essa ordem de serviço não existe") →
  `EditPartPurchaseCost(new PartPurchaseCost(valor))` (o VO já recusa negativo →
  `ValueObjectException` → 400) → `UpdateAsync` → `ServiceOrderDto`. Rota
  `PUT /service-orders/{id}/part/purchase-cost` com `{ partPurchaseCost }`. Remove a rota e o
  command antigos.
- Sem SignalR (só a tela de métricas usa esse valor).

## R5. Gráficos (revisto na implementação)

- **Revisão**: o skill dataviz desaconselha rosca para comparar partes ("ranked horizontal bars"
  comparam melhor). Implementado `app-ranked-bars` (barras horizontais, maior primeiro, uma cor,
  nome + quantidade + %, total embaixo, "Sem dados no período"). O texto abaixo era o plano original.


- **Decision**: componente próprio `app-donut-chart` em SVG (arcos com `stroke-dasharray`,
  legenda abaixo), sem biblioteca de gráficos. Antes de escrever, seguir o skill **dataviz**
  (paleta categórica validada, legenda, acessibilidade: `role="img"` + `aria-label` com os valores
  e tabela/lista oculta para leitor de tela).
- **Rationale**: dois gráficos de rosca simples não justificam Chart.js (~200 kB); tela já é
  carregada sob demanda.

## R6. Frontend

- `features/metrics/` com página, serviço e o donut; rota `/metricas` (`loadComponent`); menu
  "Métricas" → `/metricas`.
- Período: `mat-select` Mês (Janeiro…Dezembro) + Ano (ano atual e 5 anteriores), De/Até em
  `input type="date"` (como na Buscar OS); usar um modo limpa o outro.
- Cartões em grade de 4 colunas (2 no tablet, 1 no celular), título em maiúsculas, valor grande em
  vermelho, legenda curta — como a imagem, no visual atual (cartão branco, borda `#ead8d3`).
- Tabela de entregues: mesmo estilo das tabelas da Home; "Status" como chip (Reparo/Venda);
  "Valor da peça" com `matInput type="number"` + prefixo R$, grava no blur/Enter, recalcula
  `totalPartsSpend` e `netTotal` localmente após sucesso.
