# Deploy na VPS (Docker)

Sobe tudo com um comando: a API (.NET) e o sistema (Angular) atrás do Caddy, que serve o site,
repassa `/api` para a API (inclusive o tempo real) e emite o HTTPS sozinho quando há domínio.

```
navegador ──► Caddy (web, portas 80/443) ──► /api/* ──► api:8080 (.NET) ──► Azure SQL
                    ├─► arquivos do Angular
                    └─► /grafana ──► grafana:3000 ──► Loki (logs) / Prometheus (métricas)
                                                                 ▲
                                          alloy (coleta logs dos containers e métricas) ┘
```

## 1. Pré-requisitos na VPS

- Docker e o plugin Compose (`docker compose version`).
- Portas 80 e 443 liberadas no firewall.
- Para HTTPS: o domínio (ex.: `gerenciamento.tvseletronica.com`) apontando (registro A) para o IP da VPS.
- Grafana: funciona sem DNS em `http://IP_DA_VPS/grafana`. Opcional: subdomínio próprio (ex.:
  `grafana.tvseletronica.com`) com registro A para o IP da VPS, para ter HTTPS.
- Memória: a pilha de observabilidade usa até ~1,5 GB de RAM (limites por container); prefira VPS com 4 GB.
- **Azure SQL**: no Portal do Azure, servidor SQL > **Rede** > regras de firewall > adicionar o **IP público
  da VPS** (`curl -4 ifconfig.me` na VPS). Sem isso a API não conecta e fica reiniciando.

## 2. Primeira vez

```bash
git clone <url-do-repositorio> tvs-app && cd tvs-app
cp .env.example .env
nano .env            # preencha (ver tabela abaixo)
docker compose up -d --build
docker compose logs -f api   # acompanhe: migrations aplicadas e "Now listening on: http://[::]:8080"
```

Pronto: abra `https://SEU_DOMINIO` (ou `http://IP_DA_VPS` se `SITE_ADDRESS=:80`).

### Variáveis do `.env`

| Variável | Obrigatória | Para quê |
|----------|-------------|----------|
| `SITE_ADDRESS` | sim | Domínio do sistema (HTTPS automático) ou `:80` para só HTTP pelo IP |
| `HTTP_PORT`, `HTTPS_PORT` | não | Portas da VPS (padrão `80`/`443`). Se já estiverem em uso (ex.: nginx de outro site), use `8080`/`8443` e acesse `http://IP_DA_VPS:8080` |
| `DB_CONNECTION_STRING` | sim | Azure SQL (cadeia ADO.NET). Para conviver com o sistema antigo, use **o mesmo banco** dele |
| `JWT_SECRET_KEY` | sim | Chave do login (`openssl rand -base64 48`) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | não | Padrão `tvs-app` |
| `CORS_ORIGINS` | não | Sites externos que chamam a API (ex.: `https://www.tvseletronica.com`) |
| `EVOLUTION_BASE_URL`, `EVOLUTION_API_KEY`, `EVOLUTION_INSTANCE` | sim, para WhatsApp | Evolution API (outro servidor, ou `http://evolution-api:8080` se ela rodar aqui — seção 6) |
| `COMPOSE_PROFILES` | não | `whatsapp` sobe a Evolution API nesta VPS (seção 6) |
| `EVOLUTION_DB_PASSWORD`, `EVOLUTION_MANAGER_PORT` | só com a Evolution aqui | Senha do banco interno da Evolution; porta local do painel (padrão `8090`) |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | só em banco novo | Cria o primeiro usuário ao subir (se o e-mail ainda não existir) |
| `GRAFANA_ADDRESS` | não | Subdomínio próprio do Grafana (ex.: `grafana.tvseletronica.com`), HTTPS automático. Sem ele, use `/grafana` |
| `GRAFANA_ROOT_URL` | não | Endereço completo do Grafana para links absolutos (ex.: `http://IP_DA_VPS/grafana/`) |
| `GRAFANA_ADMIN_USER`, `GRAFANA_ADMIN_PASSWORD` | sim (senha) | Login do Grafana. Sem a senha o `docker compose up` recusa subir |
| `ASPNETCORE_ENVIRONMENT` | não | Deixe `Production` (em `Development` o WhatsApp vai para o número de teste) |

### Banco (Azure SQL)

- Preencha `DB_CONNECTION_STRING` com a cadeia **ADO.NET** do Portal do Azure (Banco SQL > Cadeias
  de conexão), mantendo `Encrypt=True;TrustServerCertificate=False` e os parâmetros de reconexão do
  `.env.example` (`Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=10`), que seguram
  as quedas rápidas de conexão comuns no Azure.
- Na subida a API aplica as migrations pendentes. Com o sistema antigo já atualizado, o histórico é o
  mesmo e nada é reaplicado.
- O usuário do banco precisa poder alterar o schema (migrations). Se quiser um usuário só de leitura e
  escrita para o dia a dia, rode as migrations antes com um usuário admin.
- Se a API não subir, veja `docker compose logs api`: erro de login/timeout costuma ser firewall do
  Azure (IP da VPS) ou senha.

## 3. Atualizar para uma versão nova

```bash
git pull
docker compose up -d --build
docker image prune -f        # opcional: limpa imagens antigas
```

## 4. Comandos úteis

```bash
docker compose ps                    # estado (a API tem healthcheck)
docker compose logs -f api           # logs da API
docker compose logs -f web           # logs do Caddy (HTTPS, acessos)
docker compose restart api
docker compose down                  # para tudo (volumes e dados ficam)
```

## 5. Observabilidade (logs, métricas e painéis)

Sobe junto com o resto (`docker compose up -d --build`), em 4 containers sem portas publicadas:

| Serviço | Para quê | Memória máx. | Retenção |
|---------|----------|--------------|----------|
| `alloy` | Lê os logs de todos os containers (socket do Docker) e coleta métricas da API, da VPS e dos containers | 256 MB | — |
| `loki` | Guarda os logs (volume `loki_data`) | 512 MB | 30 dias |
| `prometheus` | Guarda as métricas (volume `prometheus_data`) | 512 MB | 15 dias |
| `grafana` | Painéis e alertas, publicado pelo Caddy em `/grafana` (e no subdomínio, se houver) | 256 MB | — |

- **Acesso**: `http://IP_DA_VPS/grafana` (sem DNS), `https://SEU_DOMINIO/grafana` ou
  `https://GRAFANA_ADDRESS` (subdomínio opcional), com `GRAFANA_ADMIN_USER`/`GRAFANA_ADMIN_PASSWORD`.
  Pelo IP a conexão é HTTP: a senha trafega sem criptografia — use uma senha exclusiva do Grafana. Não há
  cadastro aberto nem acesso anônimo. A senha do `.env` só vale na primeira subida; depois, troque
  pelo próprio Grafana (ou `docker compose exec grafana grafana cli admin reset-admin-password NOVA`).
- **Painéis** (pasta *TVS*): *Visão geral* (API no ar, requisições/min, % de erros, latência,
  eventos de negócio, alertas), *Logs*, *WhatsApp* (enviadas × falhas, motivo) e *Servidor* (CPU,
  memória, disco e rede da VPS e por container).
- **Achar uma OS nos logs**: painel *Logs* → campo *Nº da OS* = número da OS (ex.: `4312`). Aparecem
  as requisições e os eventos (`service_order.delivered`, `whatsapp.sent`...). Para ver tudo de uma
  mesma requisição, copie o `CorrelationId` (detalhes da linha; também vem no cabeçalho
  `X-Correlation-Id` da resposta) e cole no campo *Busca*.
- **Alertas** (Alerting → Regras, pasta *TVS*): API fora do ar, taxa de erros > 5%, WhatsApp
  falhando (3+ em 10 min) e disco > 85%. Só aparecem no Grafana — nenhum aviso é enviado.
- **Privacidade**: telefones aparecem mascarados (`41*****0000`), e-mails de login com falha também;
  senhas, tokens e corpos de requisição nunca são registrados.
- As métricas internas da API (`/internal/metrics`) só são acessíveis na rede do Docker; pela
  internet, `/api/internal/...` responde 404.
- **A API não depende da observabilidade**: se `loki`, `prometheus`, `grafana` ou `alloy` pararem, o
  sistema segue igual e os logs continuam em `docker compose logs api`.

```bash
docker compose ps                                   # os 6 serviços "running"
docker compose logs -f alloy                        # se logs/métricas não chegam
docker compose stop loki prometheus grafana alloy   # desliga só a observabilidade
```

## 6. WhatsApp: Evolution API na mesma VPS (opcional)

O compose já traz a Evolution API (`evolution-api`, `evolution-postgres`, `evolution-redis`),
**desligada por padrão** (profile `whatsapp`). Enquanto a Evolution rodar em outro servidor, nada
disso sobe. Para trazer para esta VPS:

1. No `.env`:
   ```bash
   COMPOSE_PROFILES=whatsapp
   EVOLUTION_BASE_URL=http://evolution-api:8080     # rede interna do Docker
   EVOLUTION_API_KEY=uma-chave-longa                # openssl rand -hex 32 (vale para TVS e Evolution)
   EVOLUTION_INSTANCE=tvs                           # nome da instância (o número do WhatsApp)
   EVOLUTION_DB_PASSWORD=outra-senha-forte          # openssl rand -base64 24
   ```
2. Suba: `docker compose up -d` (cria os 3 containers e recria a `api` com a nova URL).
3. Conecte o número lendo o QR Code no painel. O painel só abre na própria VPS; do seu computador,
   use um túnel SSH:
   ```bash
   ssh -L 8090:localhost:8090 root@IP_DA_VPS
   ```
   Abra `http://localhost:8090/manager`, entre com a `EVOLUTION_API_KEY`, crie a instância com o
   nome do `EVOLUTION_INSTANCE` e leia o QR Code com o WhatsApp da loja
   (Aparelhos conectados → Conectar aparelho).
4. Teste: crie uma OS de teste e veja o painel **WhatsApp** do Grafana ou
   `docker compose logs api | grep -E "WhatsApp (enviado|falhou)"`.
5. Desconecte o número na Evolution antiga e desligue o servidor antigo.

- A sessão do WhatsApp fica nos volumes `evolution_instances`, `evolution_postgres` e
  `evolution_redis`. **Não apague esses volumes** (nem com `docker volume prune`), senão é preciso
  ler o QR Code de novo.
- A Evolution não fica exposta na internet: a API do TVS fala com ela pela rede interna.
- Memória: ~500 MB a mais (limites: Evolution 512 MB, Postgres 256 MB, Redis 128 MB).
- Logs da Evolution aparecem no Grafana (painel Logs, `{service="evolution-api"}` no Explore).

## Observações

- A API não fica exposta direto: só o Caddy (80/443). O sistema chama a API em `/api`, no mesmo
  endereço, então não precisa de CORS para ele.
- O site público (`tvseletronica`) pode continuar apontando para a API antiga. Para usar esta,
  troque o `OS_BASE` do `js/api.js` para `https://SEU_DOMINIO/api/` e inclua o domínio do site em
  `CORS_ORIGINS`.
- Certificados do HTTPS ficam no volume `caddy_data` (não apague, senão o Caddy pede de novo).
- Desenvolvimento local continua com `docker-compose.dev.yml` (`docker compose -f docker-compose.dev.yml up`).
