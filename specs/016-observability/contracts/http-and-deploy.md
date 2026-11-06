# Contracts: Observabilidade

## API
- Todas as respostas: cabeçalho `X-Correlation-Id` (o recebido, se válido, ou um novo). Exposto no CORS.
- `GET /internal/metrics` (formato Prometheus) — **só na rede interna**; `/api/internal/*` pelo Caddy → 404.
  (`/metrics` já é a rota do dashboard de negócio, que continua igual.)

## `.env` (novas variáveis)
| Variável | Obrigatória | Uso |
|----------|-------------|-----|
| `GRAFANA_ADDRESS` | para acessar o Grafana | subdomínio (ex.: `grafana.tvseletronica.com`), HTTPS pelo Caddy |
| `GRAFANA_ADMIN_USER`, `GRAFANA_ADMIN_PASSWORD` | sim | login do Grafana |

## Serviços do Compose
`api`, `web` (Caddy) + `loki`, `prometheus`, `grafana`, `alloy` — os quatro novos sem portas
publicadas; o Grafana só pelo Caddy.
