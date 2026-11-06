# Quickstart: validar Métricas

## 1. Testes e builds

```bash
dotnet test src/TVSApp.Tests     # DashboardMetricsCalculatorTests, MetricsQueryHandlerTests, UpdatePartPurchaseCost
cd web && npx ng build          # sem erro; inicial < 1 MB; chunk de métricas sob demanda
```

## 2. Roteiro manual

1. Menu **Métricas** → `/metricas`; abre no mês atual com subtítulo "Novembro/2026".
2. Conferir na mão (Buscar OS / banco) Total de ordens, Receita de reparos, Total de vendas e
   Gasto com peças do mês.
3. Trocar para outro mês → Consultar → números mudam; De/Até limpos.
4. Preencher De/Até → Consultar → subtítulo "01/11/2026 a 15/11/2026"; Mês/Ano ignorados.
   Só De → campo vermelho.
5. Tabela: digitar 120 no Valor da peça de uma linha e sair → "Valor da peça salvo."; Gasto com
   peças +120 e Total líquido −120. Valor negativo → vermelho, nada salvo.
6. Mês sem OS → zeros, "Sem dados no período" nos gráficos e mensagem na tabela.
7. Tempo médio de ciclo nunca negativo.
