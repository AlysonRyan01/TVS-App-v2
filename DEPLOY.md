# Deploy na VPS (Docker)

Sobe tudo com um comando: a API (.NET) e o sistema (Angular) atrás do Caddy, que serve o site,
repassa `/api` para a API (inclusive o tempo real) e emite o HTTPS sozinho quando há domínio.

```
navegador ──► Caddy (web, portas 80/443) ──► /api/* ──► api:8080 (.NET) ──► Azure SQL
                    └─► arquivos do Angular
```

## 1. Pré-requisitos na VPS

- Docker e o plugin Compose (`docker compose version`).
- Portas 80 e 443 liberadas no firewall.
- Para HTTPS: o domínio (ex.: `gerenciamento.tvseletronica.com`) apontando (registro A) para o IP da VPS.
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
| `DB_CONNECTION_STRING` | sim | Azure SQL (cadeia ADO.NET). Para conviver com o sistema antigo, use **o mesmo banco** dele |
| `JWT_SECRET_KEY` | sim | Chave do login (`openssl rand -base64 48`) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | não | Padrão `tvs-app` |
| `CORS_ORIGINS` | não | Sites externos que chamam a API (ex.: `https://www.tvseletronica.com`) |
| `EVOLUTION_BASE_URL`, `EVOLUTION_API_KEY`, `EVOLUTION_INSTANCE` | sim, para WhatsApp | Evolution API |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | só em banco novo | Cria o primeiro usuário ao subir (se o e-mail ainda não existir) |
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

## Observações

- A API não fica exposta direto: só o Caddy (80/443). O sistema chama a API em `/api`, no mesmo
  endereço, então não precisa de CORS para ele.
- O site público (`tvseletronica`) pode continuar apontando para a API antiga. Para usar esta,
  troque o `OS_BASE` do `js/api.js` para `https://SEU_DOMINIO/api/` e inclua o domínio do site em
  `CORS_ORIGINS`.
- Certificados do HTTPS ficam no volume `caddy_data` (não apague, senão o Caddy pede de novo).
- Desenvolvimento local continua com `docker-compose.dev.yml` (`docker compose -f docker-compose.dev.yml up`).
