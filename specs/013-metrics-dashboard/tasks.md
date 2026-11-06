# Tasks: Tela "Métricas"

**Input**: Design documents from `/specs/013-metrics-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos — a constituição (IV) e a FR-008 exigem testes dos cálculos e do valor da peça.

**Organization**: por user story (US1 ver indicadores, US2 valor da peça, US3 período sem dados e erros).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- Namespaces seguem o padrão existente (`TVS_App.*`; testes em `TVSApp.Tests.*`)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (121 verdes) e `cd web && npx ng build` (sem erro)

---

## Phase 2: Foundational (cálculo testável)

- [X] T002 Em `src/TVSApp.Application/DTOs/Dashboard/DashboardMetricsDto.cs` adicionar `decimal TotalValue` e `decimal NetTotal`; em `DeliveredProductDto.cs` adicionar `string ProductLabel`
- [X] T003 Escrever `src/TVSApp.Tests/Application/DashboardMetricsCalculatorTests.cs` (OS montadas com `ServiceOrderFactory` + métodos de domínio; venda = `Product.UpdateProduct(..., "VENDA", "", ...)`), cobrindo as definições de data-model.md: `TotalOrders`/`ActiveOrders` só reparos; `TotalRevenue` só reparos entregues aprovados com conserto; `ApprovalRate` (2 aprovadas + 1 reprovada = 66,7; sem orçamentos = 0); `AverageCycleDays` ignora OS com `DeliveryDate < EntryDate`; `TotalSales`/`TotalSalesRevenue` só vendas; `PendingDeliveryRevenue`; `TotalPartsSpend`; `TotalValue = TotalRevenue + TotalSalesRevenue`; `NetTotal = TotalValue − TotalPartsSpend`; `OrdersByStatus` e `RepairResult` sem vendas; `DeliveredProducts` com `StatusLabel` Reparo/Venda, mais recentes primeiro e `ProductLabel` = "MARCA MODELO" + " - ACESSÓRIOS" quando houver
- [X] T004 Criar `src/TVSApp.Application/Services/DashboardMetricsCalculator.cs` (`public static class`, `Build(IReadOnlyList<ServiceOrder> orders, IReadOnlyList<ServiceOrder> deliveredOrders)`) movendo o `BuildMetrics` e os `Get*Label` de `src/TVSApp.Infrastructure/Services/MetricsService.cs`, com os ajustes do research R2 (ciclo só com entrega ≥ entrada; `TotalValue`; `NetTotal`; `ProductLabel`); `MetricsService` passa a chamar o calculador. T003 deve passar

---

## Phase 3: User Story 1 — Ver os indicadores do mês (P1) 🎯 MVP

**Goal**: `/metricas` com período, 11 cartões, 2 roscas e tabela de entregues.

**Independent Test**: quickstart §2 itens 1–4 e 7.

### Backend

- [X] T005 [P] [US1] Criar `src/TVSApp.Tests/Handlers/Fakes/FakeMetricsService.cs` (`IMetricsService`; guarda o último mês/ano ou intervalo pedido; devolve um `DashboardMetricsDto` vazio)
- [X] T006 [US1] Testes em `src/TVSApp.Tests/Handlers/MetricsQueryHandlerTests.cs` para `GetDashboard(month, year, from, to)`: sem nada → mês/ano atuais; mês 0/13 ou ano 1999/2101 → 400 "Mês ou ano inválido."; só `from` ou só `to` → 400 "Informe as duas datas do período."; `to < from` → 400 "A data final não pode ser antes da inicial."; `from`+`to` válidos → chama o intervalo (datas sem hora) e ignora mês/ano; sucesso → 200 "Métricas carregadas com sucesso."
- [X] T007 [US1] Criar `src/TVSApp.Application/Handlers/Metrics/IMetricsQueryHandler.cs` e `MetricsQueryHandler.cs` conforme T006; registrar `AddScoped<IMetricsQueryHandler, MetricsQueryHandler>()` em `src/TVSApp.Api/Common/BuilderExtensions.cs`; T006 deve passar
- [X] T008 [US1] Em `src/TVSApp.Api/Endpoints/DashboardEndpoints.cs`: `GET /metrics` (`int? month, int? year, DateTime? from, DateTime? to` → `handler.GetDashboard(...)` → `ToResult()`, `.WithTags("Dashboard").RequireAuthorization()`); remover `/get-dashboard-metrics` e `/get-dashboard-metrics-by-range`; apagar `src/TVSApp.Application/Handlers/Metrics/GetDashboardMetricsHandler.cs`, `GetDashboardMetricsByRangeHandler.cs` e `src/TVSApp.Application/Requests/MetricsRequests/GetDashboardMetricsQuery.cs`, `GetDashboardMetricsByRangeQuery.cs` (`/get-abandoned-orders` fica)

### Frontend

- [X] T009 [P] [US1] Criar `web/src/app/features/metrics/models/dashboard-metrics.model.ts` (interfaces de `DashboardMetricsDto` e `DeliveredProductDto` em camelCase) e `metrics.service.ts` com `get(params: { month?, year?, from?, to? })` (`GET ${apiUrl}/metrics`, só os parâmetros preenchidos)
- [X] T010 [US1] Ler o skill **dataviz** e criar `web/src/app/features/metrics/donut-chart/donut-chart.component.{ts,html,scss}` (`app-donut-chart`, inputs `title`, `labels`, `values`): rosca em SVG (`stroke-dasharray` por fatia, ordem da API), paleta categórica validada do skill, legenda com cor + rótulo + quantidade embaixo, `role="img"` + `aria-label` com os valores; sem dados (soma 0) → "Sem dados no período"
- [X] T011 [US1] Criar `web/src/app/features/metrics/metrics.component.{ts,html,scss}` (`app-metrics`, título "Métricas"): cabeçalho com subtítulo do período ("Novembro/2026" ou "01/11/2026 a 15/11/2026") e filtros Mês (`mat-select` Janeiro…Dezembro) + Ano (ano atual e 5 anteriores) | De/Até (`input type="date"`) + botão **Consultar**; abre no mês atual; consultar por Mês/Ano limpa De/Até e vice-versa; grade de 11 cartões (4 colunas no computador, 2 no tablet, 1 no celular) na ordem e com as legendas da imagem (Total de ordens · Ordens ativas · Receita de reparos · Taxa de aprovação · Tempo médio de ciclo · Total de vendas · Receita de vendas · A receber · Gasto com peças · Valor total · Total líquido); 2 `app-donut-chart` lado a lado (Ordens por status, Resultado dos reparos); tabela "Produtos entregues no período (N)" com Ordem (#n), Cliente, Produto (`productLabel`), Tipo, Status (chip Reparo/Venda), Empresa, Entregue em, Valor total, Valor da peça (por enquanto só leitura); spinner ao carregar; visual das telas atuais
- [X] T012 [US1] Rota `{ path: 'metricas', loadComponent: () => import('./features/metrics/metrics.component').then(m => m.MetricsComponent), canActivate: [authGuard] }` em `web/src/app/app.routes.ts` e `route: '/metricas'` no item "Métricas" de `web/src/app/shared/layout/base-layout/base-layout.component.ts`

---

## Phase 4: User Story 2 — Valor da peça (P1)

**Goal**: editar o valor pago na peça na tabela e ver Gasto com peças e Total líquido mudarem.

**Independent Test**: quickstart §2 item 5.

- [X] T013 [US2] Testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` para `UpdatePartPurchaseCost(id, value)`: OS inexistente → 404 "Essa ordem de serviço não existe"; negativo → `ValueObjectException` ("O valor de compra da peça não pode ser menor que 0") sem gravar; sucesso → 200 "Valor da peça atualizado.", `PartPurchaseCost` gravado, nada mais muda
- [X] T014 [US2] Implementar `Task<BaseResponse<ServiceOrderDto?>> UpdatePartPurchaseCost(long id, decimal partPurchaseCost)` em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs`/`ServiceOrderCommandHandler.cs` (comentário citando `old-handlers/UpdatePartPurchaseCostHandler.cs`); em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs` adicionar `PUT /service-orders/{id:long}/part/purchase-cost` (corpo `record UpdatePartPurchaseCostRequest(decimal PartPurchaseCost)`), remover `/update-service-order-part-purchase-cost` e apagar `UpdatePartPurchaseCostCommand.cs`; T013 deve passar
- [X] T015 [US2] No frontend: `metrics.service.ts` ganha `updatePartPurchaseCost(id, value)`; na tabela, Valor da peça vira `matInput type="number"` (prefixo R$, `min="0"`, `step="0.01"`) que grava no blur e no Enter se o valor mudou; sucesso → snackbar "Valor da peça salvo.", atualiza a linha e recalcula `totalPartsSpend` e `netTotal` localmente; negativo → campo vermelho, não grava; erro → snackbar com a mensagem e volta ao valor anterior

---

## Phase 5: User Story 3 — Período sem dados e erros (P2)

**Goal**: mensagens de vazio e validação do período.

**Independent Test**: quickstart §2 itens 4 e 6.

- [X] T016 [US3] Em `metrics.component.{ts,html}`: só De ou só Até, ou Até antes de De → campos em vermelho e Consultar bloqueado; período sem dados → cartões com 0 / R$ 0,00 / 0%, gráficos "Sem dados no período", tabela "Nenhum produto entregue no período."; erro da API → snackbar com a mensagem, filtros mantidos

---

## Phase 6: Polish & validação

- [X] T017 [P] `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` verdes; `grep -rn "get-dashboard-metrics\|update-service-order-part-purchase-cost" src web/src` vazio
- [X] T018 [P] `cd web && npx ng build` sem erro (inicial < 1 MB; `metrics` como chunk sob demanda; SCSS de componente < 4 kB)
- [ ] T019 Roteiro manual do quickstart §2

---

## Dependencies & Execution Order

- T001 → T002 → T003 → T004 (Foundational) → US1 backend.
- US1 backend: T005 → T006 → T007 → T008. Frontend: T009 ∥ backend; T010 → T011 → T012.
- US2: T013 → T014 (backend, só depende de T001); T015 depende de T011.
- US3: T016 depende de T011.
- Polish por último.

### Parallel Opportunities

- T005 ∥ T009 ∥ T013; T010 ∥ backend de US1; T017 ∥ T018.

### Parallel Example

```text
T005 FakeMetricsService  |  T009 serviço + modelos  |  T013 testes do valor da peça  |  T010 donut
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 → tela de métricas lendo os números.
2. **US2**: valor da peça (o Total líquido passa a significar algo).
3. **US3**: vazios e validações (pequeno; pode entrar junto com T011).
4. Publicar API + web juntos (as rotas antigas de métricas saem).
