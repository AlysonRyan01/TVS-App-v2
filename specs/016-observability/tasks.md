# Tasks: Observabilidade (logs, métricas e painéis)

**Input**: Design documents from `/specs/016-observability/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-and-deploy.md, quickstart.md

**Tests**: incluídos — FR-011 (mascaramento e catálogo de eventos).

**Organization**: US1 investigar pelos logs, US2 WhatsApp, US3 saúde e uso, US4 alertas no painel.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)

---

## Phase 1: Setup

- [X] T001 Conferir a base (`dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` com 189 verdes, `docker compose config --quiet`) e adicionar os pacotes `Serilog.AspNetCore`, `Serilog.Formatting.Compact` e `prometheus-net.AspNetCore` em `src/TVSApp.Api/TVSApp.Api.csproj`

---

## Phase 2: Foundational (pilha de observabilidade e logs estruturados)

- [X] T002 [P] Testes em `src/TVSApp.Tests/Application/LogMaskTests.cs`: `LogMask.Phone("41999990000")` → `"41*****0000"`; `"5541999990000"` → `"55*******0000"`; nulo/vazio → `""`; menos de 6 dígitos → `"***"`; `LogMask.Email("maria@loja.com")` → `"m***@loja.com"`
- [X] T003 [P] Testes em `src/TVSApp.Tests/Application/BusinessEventsTests.cs`: todas as constantes de `BusinessEvents` (catálogo do data-model.md) são únicas, minúsculas e no formato `recurso.acao` (regex `^[a-z_]+\.[a-z_]+$`)
- [X] T004 Criar `src/TVSApp.Application/Observability/LogMask.cs` e `BusinessEvents.cs` (constantes do catálogo de data-model.md + `const string MeterName = "TVSApp"`); T002 e T003 devem passar
- [X] T005 Criar `src/TVSApp.Api/Common/ObservabilityExtensions.cs`: `AddObservability(builder)` configura Serilog (Produção: `RenderedCompactJsonFormatter` no console; Desenvolvimento: texto; `Information`, `Microsoft.AspNetCore`/`Microsoft.EntityFrameworkCore`/`System.Net.Http.HttpClient` em `Warning`; enriquecimento `service=tvs-api`, `environment`, `FromLogContext`) e o adaptador de `Meter` do prometheus-net (`Metrics.ConfigureMeterAdapter`, só o meter `TVSApp`); `UseObservability(app)` registra `CorrelationIdMiddleware`, `UseSerilogRequestLogging` (mensagem `HTTP {RequestMethod} {RequestPath} → {StatusCode} em {Elapsed:0} ms`; `RequestPath` = template da rota quando houver; enriquece `UserEmail` e `CorrelationId`; ignora `GET /` e `/metrics`), `UseHttpMetrics()` e `MapMetrics("/metrics")`; ligar em `src/TVSApp.Api/Program.cs` (`builder.Host.UseSerilog` antes do resto; `UseObservability` logo após `UseResponseCompression`)
- [X] T006 Criar `src/TVSApp.Api/Middlewares/CorrelationIdMiddleware.cs` (lê `X-Correlation-Id` se válido — até 64 caracteres `[A-Za-z0-9-]` — senão usa um GUID sem hífens; grava no `HttpContext.Items`, no cabeçalho de resposta e em `LogContext.PushProperty("CorrelationId", …)`) e expor `X-Correlation-Id` no CORS em `src/TVSApp.Api/Common/BuilderExtensions.cs`
- [X] T007 [P] Criar `observability/loki/loki.yaml` (single-binary, filesystem em `/loki`, schema `v13`/`tsdb`, `limits_config.retention_period: 720h`, `compactor.retention_enabled: true`, `auth_enabled: false`)
- [X] T008 [P] Criar `observability/prometheus/prometheus.yml` (sem scrapes próprios além de si; armazena o que o Alloy envia por remote write)
- [X] T009 Criar `observability/alloy/config.alloy`: `discovery.docker` + `loki.source.docker` (rótulos `container`, `service` = nome do serviço do compose; `loki.process` com `stage.json` extraindo `@l`→`level`); `prometheus.scrape` de `api:8080/metrics` (job `tvs-api`, 15s); `prometheus.exporter.unix` (host montado em `/host`) e `prometheus.exporter.cadvisor`; tudo para `prometheus.remote_write` em `http://prometheus:9090/api/v1/write` e `loki.write` em `http://loki:3100/loki/api/v1/push`
- [X] T010 Adicionar em `docker-compose.yml` os serviços `loki` (`grafana/loki:3.4.2`, `-config.file=/etc/loki/loki.yaml`, volume `loki_data`, mem 512m), `prometheus` (`prom/prometheus:v3.2.1`, `--web.enable-remote-write-receiver --storage.tsdb.retention.time=15d`, volume `prometheus_data`, mem 512m), `alloy` (`grafana/alloy:v1.7.5`, montagens `/var/run/docker.sock:ro`, `/:/host:ro,rslave`, `/sys:/host/sys:ro`, `/proc:/host/proc:ro`, `/var/lib/docker:/var/lib/docker:ro`, mem 256m) e `grafana` (`grafana/grafana:11.5.2`, ver T019), todos sem `ports`, `restart: unless-stopped`, na `appnetwork`; a `api` **não** depende deles

**Checkpoint**: `docker compose up -d --build` sobe os 6 serviços; logs da API chegam ao Loki.

---

## Phase 3: User Story 1 — Investigar um problema pelos logs (P1) 🎯 MVP

**Goal**: requisições, erros e eventos de negócio pesquisáveis por OS e correlação.

**Independent Test**: quickstart §2 itens 3–4.

- [X] T011 [US1] Criar `src/TVSApp.Api/Observability/BusinessEventFilter.cs` (`IEndpointFilter`) e a extensão `WithBusinessEvent(this RouteHandlerBuilder, string eventName)`: depois de `next`, pega o status (`IStatusCodeHttpResult` ou `Response.StatusCode`), `ServiceOrderId` da rota `id` (rotas de OS) ou do cabeçalho `X-Service-Order-Id`, `CustomerId` da rota `id` (rotas de cliente), registra `Evento {Event} OS {ServiceOrderId} → {Outcome} ({StatusCode})` (`Information` em sucesso, `Warning` em falha) e incrementa `tvs_business_events_total{event,outcome}` (`Counter<long>` no meter `TVSApp`)
- [X] T012 [US1] Aplicar `.WithBusinessEvent(BusinessEvents.X)` em todas as rotas de comando de `src/TVSApp.Api/Endpoints/**` conforme o catálogo (OS, venda, orçamento, aprovar/reprovar — Home e site —, peça, valor da peça, liberar, conserto, entrega, localização, alterar, reimprimir PDF, pagamentos e comprovante, clientes, login)
- [X] T013 [US1] Em `src/TVSApp.Api/Middlewares/ExceptionMiddleware.cs`: erros 400 de validação viram `Warning` (`Validação recusada em {RequestPath}: {Message}`) e o resto `Error` com a exceção; a resposta ao usuário não muda
- [X] T014 [US1] Login (`src/TVSApp.Api/Endpoints/Auth/AuthCommandEndpoints.cs`): log de falha de login com e-mail mascarado (`LogMask.Email`), nunca a senha

---

## Phase 4: User Story 2 — Saber se o WhatsApp foi enviado (P1)

**Goal**: enfileirada → enviada/falhou, com motivo.

**Independent Test**: quickstart §2 item 5.

- [X] T015 [US2] Em `src/TVSApp.Infrastructure/Messaging/Queue/WhatsAppQueue.cs`: ao enfileirar, log `WhatsApp enfileirado {MessageType} OS {ServiceOrderId}` (OS = propriedade `ServiceOrder` da requisição quando existir, por reflexão) e `Warning` se a fila recusar
- [X] T016 [US2] Em `src/TVSApp.Infrastructure/Messaging/BackgroundWorker/WhatsAppNotificationWorker.cs`: guardar o `BaseResponse` de cada envio do `switch` e registrar `WhatsApp enviado`/`WhatsApp falhou` com `MessageType`, `ServiceOrderId`, `Phone` (`LogMask.Phone`) e `Reason` (mensagem do serviço ou da exceção); incrementar `tvs_whatsapp_messages_total{type,outcome}`; remover o "Mensagem processada com sucesso" genérico; o envio em si não muda

---

## Phase 5: User Story 3 — Saúde e uso (P2)

**Goal**: painéis prontos.

**Independent Test**: quickstart §2 itens 2, 5, 6.

- [X] T017 [P] [US3] Criar `observability/grafana/provisioning/datasources/datasources.yaml` (Loki `uid: loki`, Prometheus `uid: prometheus`, este como padrão) e `observability/grafana/provisioning/dashboards/dashboards.yaml` (pasta "TVS", arquivos em `/var/lib/grafana/dashboards`)
- [X] T018 [US3] Criar os painéis em `observability/grafana/dashboards/`: `visao-geral.json` (API no ar, requisições/min, % 5xx, latência p50/p95 por rota, eventos de negócio por dia, lista de alertas), `logs.json` (variáveis: busca livre, OS, nível; painel de logs `{service="api"} | json`), `whatsapp.json` (enviadas × falhas por tipo e por dia; últimos logs de falha), `servidor.json` (CPU, memória, disco, rede da VPS; CPU/memória por container) — textos em pt-BR
- [X] T019 [US3] Serviço `grafana` em `docker-compose.yml`: env `GF_SECURITY_ADMIN_USER/PASSWORD` do `.env`, `GF_USERS_ALLOW_SIGN_UP=false`, `GF_AUTH_ANONYMOUS_ENABLED=false`, `GF_SERVER_ROOT_URL=https://${GRAFANA_ADDRESS}`, `GF_DEFAULT_LOCALE` pt-BR, montagens de `provisioning/` e `dashboards/`, volume `grafana_data`, mem 256m; em `web/Caddyfile` bloco `{$GRAFANA_ADDRESS:grafana.localhost} { reverse_proxy grafana:3000 }` e, no bloco do sistema, `respond /api/metrics 404` antes do proxy; repassar `GRAFANA_ADDRESS` para o `web`

---

## Phase 6: User Story 4 — Alertas no painel (P3)

**Goal**: 4 regras visíveis, sem envio.

**Independent Test**: quickstart §2 item 9.

- [X] T020 [US4] Criar `observability/grafana/provisioning/alerting/rules.yaml` com as 4 regras de data-model.md (pasta "TVS", grupo "Saúde", intervalo 1m, rótulos `severity`), sem contact points/policies novos

---

## Phase 7: Polish & validação

- [X] T021 [P] Atualizar `.env.example` (`GRAFANA_ADDRESS`, `GRAFANA_ADMIN_USER`, `GRAFANA_ADMIN_PASSWORD`) e `DEPLOY.md` (seção Observabilidade: o que sobe, como acessar o Grafana, DNS do subdomínio, retenção, como achar uma OS nos logs, consumo de RAM)
- [X] T022 [P] `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` verdes; `docker compose config --quiet`; buscar nos logs de teste por telefone completo → nenhum
- [X] T023 Subir a pilha localmente com Docker (porta alternativa) e percorrer o quickstart §2 (exceto HTTPS real)

---

## Dependencies & Execution Order

- T001 → T002/T003 → T004 → T005 → T006; T007 ∥ T008 → T009 → T010.
- US1 (T011–T014) depois de T004–T006. US2 (T015–T016) depois de T004/T005.
- US3 (T017–T019) depois de T010. US4 (T020) depois de T017.
- Polish por último.

### Parallel Opportunities

- T002 ∥ T003; T007 ∥ T008; US1 ∥ US2 ∥ (configs de US3); T017 ∥ T018; T021 ∥ T022.

### Parallel Example

```text
T002 LogMaskTests  |  T003 BusinessEventsTests  |  T007 loki.yaml  |  T008 prometheus.yml
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 → logs pesquisáveis com correlação e eventos.
2. **US2**: WhatsApp (ciclo completo nos logs).
3. **US3**: painéis e Grafana publicado.
4. **US4**: regras de alerta.

## Notas da implementação

- Métricas Prometheus em `/internal/metrics` (não `/metrics`, que já é a rota do dashboard de negócio); o Caddy responde 404 para `/api/internal/*`.
- `BusinessEvents.MeterName = "TVS"` e instrumentos `business_events_total` / `whatsapp_messages_total`: o adaptador do prometheus-net prefixa o nome do meter, então as métricas saem como `tvs_business_events_total` / `tvs_whatsapp_messages_total` (nomes do data-model). O adaptador é ligado na subida (`MeterAdapter.StartListening`) para não perder eventos antes da primeira coleta; EventCounters do .NET suprimidos.
- Alloy com `cgroup: host`, `/sys/fs/cgroup` e o socket do containerd (namespace `moby`): necessário para métricas por container quando o Docker usa o containerd image store (padrão em instalações novas). Painéis por container agrupam por `image`.
- Log de requisição também traz `ServiceOrderId` (rota `id` em `/service-orders/...` ou cabeçalho `X-Service-Order-Id`), para o filtro por OS achar a requisição e os eventos juntos.
