# Quickstart: validar Observabilidade

## 1. Testes e builds
```bash
dotnet test src/TVSApp.Tests          # LogMask, catálogo de eventos
docker compose config --quiet && docker compose up -d --build
docker compose ps                      # api, web, loki, prometheus, grafana, alloy "running"
```

## 2. Roteiro
1. `https://GRAFANA_ADDRESS` → login do `.env`; cadastro aberto indisponível.
2. Painel **Visão geral**: API "no ar", requisições/min, % erro, p95, eventos do dia, alertas.
3. No sistema, criar uma OS e entregar outra. No painel **Logs**, filtrar pela OS → log de
   requisição + `service_order.delivered success`, com o mesmo `CorrelationId` do cabeçalho de resposta.
4. Provocar um erro (ex.: entregar OS inexistente pelo Swagger/curl) → log `Error`/`Warning` com correlação.
5. Painel **WhatsApp**: `whatsapp.enqueued` → `sent`/`failed`, com telefone mascarado.
6. Painel **Servidor**: CPU/memória/disco da VPS e dos containers.
7. `curl https://SEU_DOMINIO/api/metrics` → 404. Buscar no Loki por um telefone completo → 0 resultados.
8. `docker compose stop loki prometheus grafana alloy` → sistema segue funcionando.
9. Parar a API por 3 min → alerta "API fora do ar" disparado no Grafana; subir → volta a normal.
