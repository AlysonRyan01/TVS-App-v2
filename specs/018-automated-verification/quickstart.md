# Quickstart: validar a verificação automática

## 1. Pré-requisitos
.NET 9 SDK, Docker rodando.

## 2. Verificação de fluxo (US1)
```bash
dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"
```
Esperado: todos os cenários de [contracts/verification-command.md](contracts/verification-command.md)
aprovados em ≤ 5 min. Conferir que nenhuma requisição saiu para a internet (o handler falso
registra tudo; `BaseUrl` aponta para `evolution.invalid`).

Prova de detecção (SC-003): comentar o enfileiramento de `SendDeviceReadyMessageRequest` no
`RegisterRepair` → rodar de novo → cenário "Conserto envia aparelho pronto" falha com
"esperado … / obtido nenhuma mensagem". Desfazer.

Repetibilidade (SC-006): rodar 3 vezes seguidas → mesmo resultado.

## 3. Monitor de conexão (US2) — local com a Evolution do compose
```bash
COMPOSE_PROFILES=whatsapp docker compose up -d evolution-api   # instância criada mas não conectada
# API apontando para http://evolution-api:8080, instância "tvs"
```
- `/internal/metrics` mostra `tvs_whatsapp_connected 0` e log "WhatsApp conexão: desconectado".
- Parar a Evolution → log "indisponível"; Grafana: alerta "WhatsApp desconectado" em ≤ 10 min.
- Sem `EVOLUTION_BASE_URL` → sem métrica e sem alerta.

## 4. Envio real de teste (US3)
- Sem `WHATSAPP_TEST_PHONE`: `docker compose exec api curl -s -X POST localhost:8080/internal/whatsapp/test-message` → 400 com aviso.
- Com número e Evolution conectada → 200 e mensagem "🧪 Mensagem de teste…" no celular.
- `curl http://IP/api/internal/whatsapp/test-message` (de fora) → 404.

## 5. Gates
`dotnet build TVSApp.sln`; `dotnet test src/TVSApp.Tests` (unitários, sem Docker) verdes.
