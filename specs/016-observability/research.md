# Research: Observabilidade

Estado atual: logs só no console (`ILogger` padrão) em 3 lugares — `ExceptionMiddleware`,
`WhatsAppQueue` e `WhatsAppNotificationWorker`. O worker registra "processada com sucesso" mesmo
quando o `WhatsappService` devolve erro (só o contato do site confere o resultado). Deploy em Docker
Compose com Caddy na frente (feature de deploy); banco no Azure.

## R1. Como os logs chegam ao Loki

- **Decision**: a API escreve **JSON estruturado no stdout** (Serilog, formato compacto), e o
  **Grafana Alloy** lê os logs dos containers pelo socket do Docker (`loki.source.docker`), extrai
  `level`, `service` e o JSON, e envia ao Loki.
- **Rationale**: a API não depende do Loki (FR-010): se a pilha cair, o app segue igual e os logs
  continuam em `docker compose logs`. O mesmo coletor pega Caddy, Grafana etc.
- **Alternatives**: sink do Serilog direto para o Loki (acopla o app ao Loki); Promtail
  (descontinuado em favor do Alloy).

## R2. Serilog na API

- **Decision**: `Serilog.AspNetCore` + `Serilog.Formatting.Compact`, configurado em código:
  - Produção: `RenderedCompactJsonFormatter` no console; Desenvolvimento: texto legível (como hoje).
  - Níveis: `Information`; `Microsoft.AspNetCore` e `Microsoft.EntityFrameworkCore` em `Warning`.
  - Enriquecimento: `service=tvs-api`, ambiente, `CorrelationId`, `UserEmail` (quando logado).
  - `UseSerilogRequestLogging` com um log por requisição: método, rota (template), status,
    duração, usuário, correlação; **sem** query string nem corpo; ignora `GET /` e `/metrics`.

## R3. Identificador de correlação

- **Decision**: middleware `CorrelationIdMiddleware`: usa o `X-Correlation-Id` recebido (se válido,
  até 64 caracteres alfanuméricos/hífen) ou gera um; devolve no cabeçalho de resposta; empurra
  para o `LogContext`. CORS expõe o cabeçalho.

## R4. Eventos de negócio

- **Decision**: um **filtro de endpoint** (`BusinessEventFilter`) aplicado com
  `.WithBusinessEvent("service_order.delivered")` nas rotas de comando: depois da execução, registra
  `Evento {Event} OS {ServiceOrderId} → {Outcome} ({StatusCode})` (`Outcome` = `success`/`failure`)
  e incrementa o contador `tvs_business_events_total{event,outcome}`. O número da OS vem da rota
  (`id`) ou do cabeçalho `X-Service-Order-Id` (criar OS/venda); cliente pelo `id` da rota.
- **Rationale**: cobre todos os eventos sem mexer nos handlers nem nos 189 testes; o nome do evento
  fica junto da rota.
- **Catálogo de eventos** (constantes em `TVS_App.Application.Observability.BusinessEvents`):
  ver data-model.md.

## R5. WhatsApp

- **Decision**:
  - `WhatsAppQueue.Enqueue` registra `whatsapp.enqueued` (tipo, OS).
  - `WhatsAppNotificationWorker` passa a **conferir o resultado** de cada envio e registra
    `whatsapp.sent` ou `whatsapp.failed` (tipo, OS, telefone mascarado, motivo) + contador
    `tvs_whatsapp_messages_total{type,outcome}`. Só o log muda; o envio continua igual.
  - Número da OS: propriedade `ServiceOrder` da requisição, quando existe.

## R6. Métricas

- **Decision**: `prometheus-net.AspNetCore`:
  - `UseHttpMetrics()` (requisições, duração por rota/status) e `/metrics` na API;
  - contadores de negócio via `System.Diagnostics.Metrics` (`Meter "TVS"`, BCL, sem pacote na
    Application) exportados pelo adaptador de Meter do prometheus-net.
  - `/metrics` **não é publicado**: o Caddy responde 404 para `/api/metrics`; o Alloy coleta pela
    rede interna (`api:8080/metrics`).
- **Servidor e containers**: componentes do Alloy `prometheus.exporter.unix` (CPU, memória, disco,
  rede da VPS) e `prometheus.exporter.cadvisor` (containers).
- **Envio**: o Alloy faz `remote_write` para o Prometheus (receptor ligado), que só armazena
  (retenção 15 dias). Assim uma configuração (Alloy) descreve toda a coleta.

## R7. Retenção e recursos

- Loki: `retention_period: 720h` (30 dias) com o compactor ligado; armazenamento em volume.
- Prometheus: `--storage.tsdb.retention.time=15d`.
- Limites de memória por container (Loki 512M, Prometheus 512M, Grafana 256M, Alloy 256M).

## R8. Grafana

- Provisionado por arquivos (`observability/grafana/provisioning`): datasources (Loki, Prometheus),
  4 painéis JSON ("Visão geral", "Logs", "WhatsApp", "Servidor") e as regras de alerta (YAML) sem
  ponto de contato (Clarification Q1).
- Admin e senha do `.env`; `GF_USERS_ALLOW_SIGN_UP=false`; `GF_SERVER_ROOT_URL` = subdomínio.
- Publicação: novo bloco no Caddyfile `{$GRAFANA_ADDRESS}` → `grafana:3000`; sem
  `GRAFANA_ADDRESS` no `.env`, usa `grafana.localhost` (não acessível de fora). Loki, Prometheus e
  Alloy sem portas publicadas.

## R9. Mascaramento

- `LogMask.Phone("41999990000")` → `41*****0000`; `LogMask.Email` → `a***@dominio.com`.
  Telefones só aparecem mascarados (WhatsApp); documentos nunca. Senhas/tokens não são logados (sem
  corpo de requisição). Testes em `TVSApp.Tests`.
