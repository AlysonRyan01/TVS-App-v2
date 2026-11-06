# TVS-App

Sistema de gestão da assistência técnica **TVS Eletrônica**: ordens de serviço de conserto e de
venda, orçamentos (inclusive aprovação pelo próprio cliente), peças, pagamentos, comprovantes em
PDF, dashboard de métricas e avisos automáticos por WhatsApp — com atualização em tempo real entre
as telas e observabilidade completa (logs, métricas, painéis e alertas).

## Índice

- [Sobre o projeto](#sobre-o-projeto)
- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Como rodar localmente](#como-rodar-localmente)
- [Testes](#testes)
- [API](#api)
  - [Convenções](#convenções)
  - [Autenticação](#autenticação)
  - [Clientes e endereço](#clientes-e-endereço)
  - [Ordens de serviço (consultas)](#ordens-de-serviço-consultas)
  - [Ordens de serviço (comandos)](#ordens-de-serviço-comandos)
  - [Pagamentos](#pagamentos)
  - [Métricas do dashboard](#métricas-do-dashboard)
  - [Rotas públicas](#rotas-públicas)
  - [Tempo real](#tempo-real)
  - [Rotas internas](#rotas-internas)
- [Modelos de dados](#modelos-de-dados)
  - [Objetos de entrada](#objetos-de-entrada)
  - [DTOs de saída](#dtos-de-saída)
  - [Enumerações](#enumerações)
- [Notificações de WhatsApp](#notificações-de-whatsapp)
- [Docker e produção](#docker-e-produção)
- [Observabilidade](#observabilidade)
- [Contribuindo](#contribuindo)
- [Licença](#licença)

---

## Sobre o projeto

Feito para o balcão e a bancada de uma assistência técnica de TVs e eletrônicos. Cobre o ciclo
inteiro de um aparelho:

1. **Entrada** — cadastro do cliente (com busca de endereço por CEP) e abertura da OS, que gera o
   PDF de entrada e avisa o cliente por WhatsApp.
2. **Orçamento** — solução, valor, garantia e resultado (tem conserto / não tem / sem defeito). O
   cliente recebe o orçamento por WhatsApp com um **código de segurança** e pode aprovar ou
   reprovar sozinho pelo site; a loja também pode registrar a resposta.
3. **Peça** — registro da compra da peça e do seu custo.
4. **Conserto** — OS consertada, cliente avisado de que o aparelho está pronto.
5. **Entrega** — PDF de saída com garantia e aviso de garantia por WhatsApp. Orçamentos
   reprovados são liberados para retirada.
6. **Pagamentos** — vários pagamentos por OS, com comprovante em PDF (valor por extenso).
7. **Vendas** — OS de venda de produto, com PDF e aviso por WhatsApp.

Além disso: localização física do produto na loja, busca de OS por vários filtros, listas por
etapa (orçamento pendente, aguardando resposta, aprovadas, reprovadas, aguardando peça, prontas),
reimpressão de PDF, dashboard financeiro/operacional por mês ou período, contato do site público
encaminhado ao WhatsApp da loja e atualização automática das telas abertas (SignalR).

## Tecnologias

| Área | Tecnologias |
|------|-------------|
| Backend | .NET 9, ASP.NET Core (minimal APIs), Clean Architecture |
| Banco | SQL Server / Azure SQL com EF Core e migrations |
| Autenticação | ASP.NET Core Identity + JWT (access token) e refresh token |
| Tempo real | SignalR (`/osHub`) |
| PDFs | QuestPDF |
| WhatsApp | Evolution API, enviado por uma fila em background |
| Frontend | Angular 22 (TypeScript 6) em `web/` |
| Infraestrutura | Docker Compose, Caddy (proxy reverso com HTTPS automático) |
| Observabilidade | Serilog (JSON), prometheus-net, Grafana Alloy, Loki, Prometheus, Grafana |
| Testes | MSTest |

## Arquitetura

```text
                 ┌──────────────────────── VPS (Docker Compose) ───────────────────────────┐
navegador ─────► │ web (Caddy :80/:443) ──► /api/* ──► api (.NET :8080) ──► Azure SQL       │
site público ──► │   ├─ arquivos do Angular              ├─ /osHub (SignalR, tempo real)     │
                 │   └─ /grafana ──► grafana              └─ fila ──► worker ──► Evolution API│
                 │                             ▲                                (WhatsApp)   │
                 │        alloy ──► loki (logs) / prometheus (métricas)                       │
                 └──────────────────────────────────────────────────────────────────────────┘
```

Camadas do backend (as dependências apontam sempre para dentro):

```text
TVSApp.Api ──► TVSApp.Infrastructure ──► TVSApp.Application ──► TVSApp.Domain
(composição,     (EF Core, repositórios,    (handlers, comandos,     (entidades, value objects,
 endpoints,       JWT, PDFs, WhatsApp,       DTOs, interfaces)        enums, regras de negócio)
 middlewares)     fila, serviços externos)
```

- **Domain** não depende de nada (nem de EF Core).
- **Application** depende só do Domain e define as interfaces (repositórios, serviços).
- **Infrastructure** implementa essas interfaces.
- **Api** só compõe: injeção de dependência, configuração, endpoints e middlewares — sem regra de
  negócio.

Fluxo típico de uma escrita: endpoint → handler (Application) valida o comando → entidade do
Domain aplica a regra → repositório grava → mensagens de WhatsApp vão para a fila → o endpoint
avisa as telas pelo SignalR (`"Atualizar"`) e registra um evento de negócio nos logs/métricas.

## Estrutura de pastas

```text
.
├── src/
│   ├── TVSApp.Domain/          # entidades (ServiceOrder, Customer, Payment...), enums, value objects, regras
│   ├── TVSApp.Application/     # handlers, comandos/requests, DTOs, interfaces, observabilidade (eventos, LogMask)
│   ├── TVSApp.Infrastructure/  # EF Core (DbContext, migrations), repositórios, JWT, PDFs, WhatsApp, fila e worker
│   ├── TVSApp.Api/             # Program.cs, endpoints, middlewares, SignalR, Dockerfile
│   ├── TVSApp.Tests/           # testes unitários MSTest (Domain, Application, Handlers, Infrastructure)
│   └── TVSApp.IntegrationTests/ # verificação ponta a ponta: API real + SQL Server descartável + WhatsApp falso
├── web/                        # sistema em Angular + Caddyfile + Dockerfile (Caddy servindo o build)
├── observability/              # configs de Alloy, Loki, Prometheus e Grafana (painéis e alertas)
├── scripts/                    # utilitários de operação (ex.: teste-whatsapp.sh)
├── specs/                      # especificações de cada feature (Spec Kit): spec, plan, tasks...
├── old-handlers/               # handlers da versão antiga, mantidos só como referência
├── .specify/                   # constituição do projeto e scripts do Spec Kit
├── docker-compose.yml          # produção: api, web, loki, prometheus, alloy, grafana
├── docker-compose.dev.yml      # desenvolvimento: SQL Server local + API com hot reload
├── .env.example                # variáveis de ambiente de produção
├── DEPLOY.md                   # guia de deploy na VPS
└── TVSApp.sln
```

## Como rodar localmente

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Node.js 22 + npm
- Docker (para o SQL Server local)

### 1. Banco de dados local

```bash
docker compose -f docker-compose.dev.yml up -d sqlserver
```

Sobe um SQL Server 2022 em `localhost:1433` (usuário `sa`; a senha está no
`docker-compose.dev.yml` e **serve só para desenvolvimento**).

### 2. Configurar a API

As configurações ficam em `src/TVSApp.Api/appsettings*.json` e podem ser sobrescritas por
`dotnet user-secrets` ou variáveis de ambiente (`Secao__Chave`). Exemplo com user-secrets:

```bash
cd src/TVSApp.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=tvs;User ID=sa;Password=SUA_SENHA_LOCAL;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:SecretKey" "uma-chave-local-longa-com-pelo-menos-32-caracteres"
dotnet user-secrets set "Jwt:Issuer" "tvs-app"
dotnet user-secrets set "Jwt:Audience" "tvs-app"
dotnet user-secrets set "Cors:Origins" "http://localhost:4200"
dotnet user-secrets set "Admin:Email" "admin@exemplo.com"
dotnet user-secrets set "Admin:Password" "Senha@123"
# opcional (WhatsApp): EvolutionApiSettings:BaseUrl, EvolutionApiSettings:ApiKey, EvolutionApiSettings:Instance
cd ../..
```

| Chave | Para quê |
|-------|----------|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience` | assinatura e validação dos tokens |
| `Cors:Origins` | origens liberadas, separadas por vírgula — inclua `http://localhost:4200` para o `ng serve` |
| `EvolutionApiSettings:BaseUrl`, `:ApiKey`, `:Instance` | envio de WhatsApp |
| `Admin:Email`, `Admin:Password` | cria o primeiro usuário na subida, se o e-mail ainda não existir |

> Em `Development` as mensagens de WhatsApp vão para o número de teste, nunca para o cliente.

### 3. Rodar a API

```bash
dotnet run --project src/TVSApp.Api
```

- Ouve em `http://localhost:5001` (perfil `http` do `launchSettings.json`).
- Aplica as migrations pendentes e cria o usuário inicial ao subir.
- `GET http://localhost:5001/` responde `RUNNING`.
- **Swagger**: `http://localhost:5001/swagger` (só em `Development`).

### 4. Rodar o sistema web

```bash
cd web
npm ci
npm start          # ng serve em http://localhost:4200
```

Em desenvolvimento o Angular chama a API em `http://localhost:5001`
(`web/src/environments/environment.development.ts`); em produção usa `/api` no mesmo endereço.

### Alternativa: API também no Docker

```bash
docker compose -f docker-compose.dev.yml up
```

Sobe o SQL Server e a API (com hot reload, código montado de `./src`) em `http://localhost:5001`.
Configure as demais chaves (JWT, CORS...) no `appsettings.Development.json` ou em variáveis do
serviço `api`.

### Pilha completa como em produção

Veja [Docker e produção](#docker-e-produção).

## Testes

```bash
dotnet test src/TVSApp.Tests
```

Testes MSTest das regras de negócio, sem banco nem rede:

| Pasta | O que cobre |
|-------|-------------|
| `Domain/` | documentos (CPF/CNPJ), resposta de orçamento, compra de peça, conserto, entrega, liberação de reprovados |
| `Handlers/` | handlers de OS (comandos e consultas), clientes e métricas |
| `Application/` | cálculo do dashboard, comprovante de pagamento, valor por extenso, mascaramento de logs, catálogo de eventos |
| `Infrastructure/` | estado da conexão do WhatsApp e mensagem de teste (Evolution simulada) |

### Verificação ponta a ponta (substitui o teste manual com cliente falso)

```bash
dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"
```

Precisa só do **Docker rodando** (não usa `.env`, Azure SQL nem a Evolution real). Em ~1 minuto:

1. sobe um SQL Server descartável e a API real (ambiente `Production`), aplicando as migrations do zero;
2. percorre o ciclo pela API, como o sistema faria: login, cliente, abrir OS (PDF), orçamento,
   aprovação pela loja e pelo link do cliente (inclusive código errado), reprovação e liberação,
   Philco/Britania, peça, conserto, entrega (PDF), pagamentos e comprovante, venda, contato do site,
   busca de ordens, grade de clientes (nome sem acento, telefone com/sem pontuação, documento,
   paginação), listas, métricas e envio de teste;
3. confere, em cada etapa, **a mensagem de WhatsApp exata** que seria enviada (número, nº da OS,
   texto) — ou que nenhuma foi gerada quando não deve.

**Nada é enviado de verdade:** a Evolution é substituída por um falso que só registra as mensagens, e
o endereço configurado é `http://evolution.invalid` (mesmo que algo falhasse, não há para onde
enviar). No fim, uma linha por cenário (`Aprovado …` / `Com falha …`); numa falha, a etapa e o
"esperado / obtido". Rode antes de publicar uma atualização.

O envio **real** (para conferir que chega no celular) é feito sob demanda na VPS — ver
[Rotas internas](#rotas-internas).

Build do frontend:

```bash
cd web && npm run build
```

---

## API

### Convenções

- **Endereço**: em produção a API fica atrás do Caddy em `https://SEU_DOMINIO/api/...` (o prefixo
  `/api` é removido antes de chegar à API). Localmente, direto em `http://localhost:5001/...`.
  Os caminhos abaixo são os da API, sem o `/api`.
- **JSON** em `camelCase`; datas em ISO 8601 (`2026-11-06T14:30:00`); valores monetários como
  número decimal (`350.00`).
- **Enums são números** (ex.: `"productType": 1` = TV). Veja [Enumerações](#enumerações).
- **Login**: rotas marcadas com "Sim" exigem `Authorization: Bearer <accessToken>`; sem ele a
  resposta é `401` sem corpo.
- **Correlação**: toda resposta traz `X-Correlation-Id`. Se você enviar esse cabeçalho (até 64
  caracteres `A-Z a-z 0-9 -`), o mesmo valor é usado nos logs.

#### Envelope de resposta

Quase todas as rotas devolvem o mesmo envelope:

```json
{
  "data": { "...": "..." },
  "message": "Ordem de serviço atualizada com sucesso!",
  "code": 200,
  "isSuccess": true
}
```

Erro (mesmo formato, `data` nulo):

```json
{ "data": null, "message": "Essa ordem de serviço não existe", "code": 404, "isSuccess": false }
```

As exceções são as rotas que devolvem **PDF** (`application/pdf`) em caso de sucesso — criar OS,
criar venda, entregar, reimprimir e comprovante de pagamento — e `GET /`.

#### Códigos HTTP

| Código | Quando |
|--------|--------|
| `200` | sucesso |
| `400` | validação recusada (ex.: "O modelo da ordem de serviço não pode estar vazio"), regra de negócio violada ou credenciais inválidas no login |
| `409` | orçamento já respondido ("Esse orçamento já foi respondido.") |
| `401` | sem token ou token inválido/expirado (rotas protegidas) |
| `404` | registro não encontrado (o envelope traz a mensagem) |
| `500` | erro inesperado (a mensagem vem no envelope; detalhes nos logs pelo `X-Correlation-Id`) |

> O campo `code` do envelope normalmente é igual ao status HTTP. Exceção conhecida: login
> inválido responde HTTP `400` com `code: 401`.

#### Formato das rotas

Nas tabelas, **Login** = `Sim` (Bearer obrigatório) ou `Não` (pública). **Tempo real** indica que
a rota avisa as telas pelo SignalR. **Evento** é o evento de negócio registrado nos logs e
métricas (ver [Observabilidade](#observabilidade)).

---

### Autenticação

A sessão tem duas partes:

| Parte | Validade | Onde fica |
|-------|----------|-----------|
| **Token de acesso** (JWT) | 1 hora | corpo do login/refresh; o sistema web guarda **só em memória** e envia em `Authorization: Bearer` |
| **Sessão longa** (refresh token) | 30 dias | **somente** no cookie `tvs_refresh` — `HttpOnly` (scripts da página não leem), `Secure` (só HTTPS), `SameSite=Strict` (outros sites não usam), `Path=/` |

Fluxo:

1. `POST /auth/login` com e-mail e senha → corpo com `accessToken` + cookie `tvs_refresh`.
2. Envie `Authorization: Bearer <accessToken>` nas rotas protegidas.
3. Ao abrir o sistema (ou quando o acesso expira), `POST /auth/refresh` **sem corpo** — o navegador
   envia o cookie — devolve um novo `accessToken`. Assim, fechar a aba, abrir outra aba ou reiniciar
   o navegador **não pede login** por até 30 dias.
4. `POST /auth/logout` revoga a sessão e apaga o cookie; o sistema avisa as outras abas abertas, que
   voltam para a tela de login.

> **Só funciona com HTTPS** (domínio). Pelo IP sem HTTPS o navegador recusa o cookie `Secure`: o
> login funciona, mas não é lembrado (nem num F5). Em desenvolvimento, `http://localhost` é aceito.
> O sistema web chama as rotas `/auth/*` com `withCredentials: true`.

| Método | Rota | Login | Descrição | Evento |
|--------|------|-------|-----------|--------|
| `POST` | `/auth/login` | Não | Entra no sistema: token de acesso no corpo + cookie de sessão | `auth.login` |
| `POST` | `/auth/logout` | Não (usa o cookie) | Revoga a sessão e apaga o cookie | — |
| `POST` | `/auth/refresh` | Não (usa o cookie) | Gera um novo token de acesso | — |
| `POST` | `/auth/register` | Sim | Cria um novo usuário (só um usuário logado cria outro) | — |

#### `POST /auth/login`

- Corpo: [`LoginRequest`](#loginrequest)
- `200`: envelope com [`AccessTokenDto`](#accesstokendto), "Login realizado com sucesso!", e
  `Set-Cookie: tvs_refresh=…; expires=+30 dias; path=/; secure; samesite=strict; httponly`.
  O corpo **não** traz o refresh token.
- `400` (`code: 401`): "Email ou senha inválidos"

```bash
curl -c cookies.txt -X POST https://SEU_DOMINIO/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@exemplo.com","password":"Senha@123"}'
```

#### `POST /auth/refresh`

- Sem corpo; lê o cookie `tvs_refresh`.
- `200`: envelope com `data` = novo token de acesso, "Token renovado com sucesso!"
- `401` (`code: 401`): "Sessão expirada. Entre novamente." — sem cookie, sessão vencida ou
  revogada; a resposta também apaga o cookie.

```bash
curl -b cookies.txt -X POST https://SEU_DOMINIO/api/auth/refresh
```

#### `POST /auth/logout`

- Sem corpo e sem login; revoga a sessão do cookie (se houver) e apaga o cookie.
- `200`: "Logout realizado com sucesso!" (também quando não há sessão).

#### `POST /auth/register`

- Corpo: [`LoginRequest`](#loginrequest) (e-mail e senha do novo usuário; a senha segue as regras
  padrão do Identity)
- `200`: "Usuário criado com sucesso!" (`data` nulo — não devolve tokens nem altera a sessão de quem
  está logado)
- `400`: "Esse usuário já existe" (`code: 401`) ou lista de erros de senha em `data` (`code: 500`)

---

### Clientes e endereço

| Método | Rota | Login | Descrição | Tempo real | Evento |
|--------|------|-------|-----------|------------|--------|
| `GET` | `/customers` | Sim | Busca rápida pelo nome (lista simples, usada ao escolher o cliente da OS/venda) | — | — |
| `GET` | `/customers/search` | Sim | Grade de clientes: filtros combinados e paginação (tela **Clientes**) | — | — |
| `POST` | `/customers` | Sim | Cadastra cliente | Sim | `customer.created` |
| `PUT` | `/customers/{id}` | Sim | Atualiza cliente | Sim | `customer.updated` |
| `GET` | `/addresses/{cep}` | Sim | Busca endereço pelo CEP | — | — |

#### `GET /customers?name={nome}`

- Query: `name` (obrigatório) — parte do nome.
- `200`: envelope com lista de [`CustomerDto`](#customerdto).

#### `GET /customers/search`

- Query: [`CustomerSearchQuery`](#customersearchquery) — `name`, `phone`, `document` **combinados**
  (todos opcionais) e paginação (`pageSize` padrão `20`, máximo `100`). Sem filtros → todos, cadastrados
  por último primeiro.
- Nome sem diferenciar maiúsculas **nem acentos** (`joao` encontra `JOÃO`); telefone compara só os
  dígitos, no principal ou no secundário (encontra números gravados com ou sem pontuação); CPF/CNPJ com
  ou sem pontuação.
- `200`: envelope com [`PaginatedResult`](#paginatedresult) de [`CustomerDto`](#customerdto) e mensagem
  "N clientes encontrados." / "Nenhum cliente encontrado.".
- `400`: "Digite pelo menos 3 letras do nome." · "Digite pelo menos 4 números do telefone." ·
  "Digite pelo menos 3 caracteres do CPF/CNPJ." · "Paginação inválida."

```bash
curl "http://localhost:5001/customers/search?phone=(41)%2099999-0000" -H "Authorization: Bearer $TOKEN"
```

#### `POST /customers`

- Corpo: [`CreateCustomerCommand`](#createcustomercommand)
- `200`: envelope com o [`CustomerDto`](#customerdto) criado.
- `400`: "O nome do cliente não pode estar vazio" / "O telefone do cliente não pode estar vazio".

#### `PUT /customers/{id}`

- Rota: `id` do cliente. Corpo: [`UpdateCustomerCommand`](#updatecustomercommand)
- `200`: envelope com o [`CustomerDto`](#customerdto) atualizado.

#### `GET /addresses/{cep}`

- Rota: `cep` (só números ou com hífen).
- `200`: envelope com [`AddressDto`](#addressdto) (usado para preencher o cadastro).

---

### Ordens de serviço (consultas)

| Método | Rota | Login | Descrição | Evento |
|--------|------|-------|-----------|--------|
| `GET` | `/service-orders/pending-estimate` | Sim | OS com orçamento pendente | — |
| `GET` | `/service-orders/awaiting-response` | Sim | Orçamentos aguardando resposta do cliente | — |
| `GET` | `/service-orders/approved` | Sim | Orçamentos aprovados (a consertar) | — |
| `GET` | `/service-orders/rejected` | Sim | Orçamentos reprovados (a liberar) | — |
| `GET` | `/service-orders/awaiting-part` | Sim | Aguardando peça | — |
| `GET` | `/service-orders/ready-for-pickup` | Sim | Prontas para entrega | — |
| `GET` | `/service-orders/search` | Sim | Busca com filtros | — |
| `GET` | `/service-orders/{id}` | Sim | Detalhe completo da OS | — |
| `GET` | `/service-orders/{id}/pdf` | Sim | Reimprime o PDF da OS | `service_order.pdf_reprinted` |

#### Listas por etapa

`GET /service-orders/{etapa}?pageNumber=1&pageSize=25`

- Query: `pageNumber` (padrão `1`), `pageSize` (padrão `25`).
- `200`: envelope com [`PaginatedResult`](#paginatedresult) de itens da etapa:

| Etapa | Item | Campos |
|-------|------|--------|
| `pending-estimate` | `PendingEstimateServiceOrder` | `number`, `customer`, `company`, `model`, `defect`, `enteredAt` |
| `awaiting-response` | `AwaitingResponseServiceOrder` | `number`, `customer`, `model`, `solution`, `amount`, `evaluatedAt` |
| `approved` | `ApprovedServiceOrder` | `number`, `company`, `customer`, `model`, `solution`, `approvedAt` |
| `rejected` | `RejectedServiceOrder` | `number`, `customer`, `model`, `solution`, `rejectedAt` |
| `awaiting-part` | `AwaitingPartServiceOrder` | `number`, `customer`, `model`, `solution`, `partPurchasedAt` |
| `ready-for-pickup` | `ReadyForPickupServiceOrder` | `number`, `customer`, `model`, `repairedAt`, `repairResult` (texto), `repairStatus` (texto) |

`number` é o número da OS; `company` é o nome da empresa ([`EEnterprise`](#eenterprise)) por extenso.

#### `GET /service-orders/search`

- Query: [`ServiceOrderSearchQuery`](#serviceordersearchquery) — **um filtro por vez** (número, cliente,
  modelo, série **ou** período), mais a paginação (`pageSize` padrão `20`, máximo `100`).
- `200`: envelope com [`PaginatedResult`](#paginatedresult) de [`ServiceOrderSearchItem`](#serviceordersearchitem)
  e mensagem "N ordens encontradas." ou "Nenhuma ordem de serviço encontrada.".
- `400`: "Preencha um filtro por vez." · "Informe um número de OS válido." · "Digite pelo menos 3
  letras do cliente." · "A data final não pode ser antes da inicial." · "Paginação inválida."

```bash
curl "http://localhost:5001/service-orders/search?customer=maria" -H "Authorization: Bearer $TOKEN"
curl "http://localhost:5001/service-orders/search?from=2026-01-01&to=2026-12-31&pageSize=50" -H "Authorization: Bearer $TOKEN"
```

#### `GET /service-orders/{id}`

- `200`: envelope com [`ServiceOrderDto`](#serviceorderdto). `404`: "Essa ordem de serviço não existe".

#### `GET /service-orders/{id}/pdf`

- `200`: `application/pdf` (`ordem_servico_{id}.pdf`) — a via atual da OS.

---

### Ordens de serviço (comandos)

| Método | Rota | Login | Descrição | Tempo real | Evento |
|--------|------|-------|-----------|------------|--------|
| `POST` | `/service-orders` | Sim | Abre OS de conserto (PDF de entrada) | Sim | `service_order.created` |
| `POST` | `/service-orders/sales` | Sim | Registra venda (PDF da venda) | Sim | `sale.created` |
| `PUT` | `/service-orders/{id}` | Sim | Edita todos os dados da OS | Sim | `service_order.updated` |
| `PUT` | `/service-orders/{id}/estimate` | Sim | Registra o orçamento | Sim | `estimate.added` |
| `PUT` | `/service-orders/{id}/estimate/approve` | Sim | Registra aprovação do orçamento | Sim | `estimate.approved` |
| `PUT` | `/service-orders/{id}/estimate/reject` | Sim | Registra reprovação do orçamento | Sim | `estimate.rejected` |
| `PUT` | `/service-orders/{id}/ready-for-pickup` | Sim | Libera para retirada um orçamento reprovado | Sim | `service_order.ready_for_pickup` |
| `PUT` | `/service-orders/{id}/part/purchase` | Sim | Registra a compra da peça | Sim | `part.purchased` |
| `PUT` | `/service-orders/{id}/part/purchase-cost` | Sim | Altera o custo de compra da peça | — | `part.purchase_cost_updated` |
| `PUT` | `/service-orders/{id}/repair` | Sim | Registra o conserto | Sim | `service_order.repaired` |
| `PUT` | `/service-orders/{id}/delivery` | Sim | Registra a entrega (PDF de saída) | Sim | `service_order.delivered` |
| `PUT` | `/service-orders/{id}/product/location` | Sim | Define onde o produto está na loja | Sim | `service_order.location_updated` |

#### `POST /service-orders`

- Corpo: [`CreateServiceOrderCommand`](#createserviceordercommand)
- `200`: `application/pdf` (`ordem_servico_{id}.pdf`) + cabeçalho **`X-Service-Order-Id`** com o
  número da OS criada.
- Efeitos: WhatsApp de boas-vindas ao cliente.

```bash
curl -X POST http://localhost:5001/service-orders \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"customerId":1,"productBrand":"LG","productModel":"42LB5600","productSerialNumber":"SN123","productDefect":"Sem imagem","accessories":"Controle","productType":1,"enterprise":1}' \
  -D - -o os.pdf
```

#### `POST /service-orders/sales`

- Corpo: [`CreateSalesServiceOrderCommand`](#createsalesserviceordercommand)
- `200`: `application/pdf` (`venda_{id}.pdf`) + cabeçalho `X-Service-Order-Id`.
- Efeitos: WhatsApp de venda ao cliente.

#### `PUT /service-orders/{id}`

- Corpo: [`EditServiceOrderCommand`](#editserviceordercommand) (substitui todos os campos editáveis).
  Com o bloco opcional `estimate`, também corrige situação, resposta do orçamento, resultado,
  mensagem do orçamento e custo de compra da peça — correção manual, **sem** WhatsApp nem notificação.
- `200`: envelope com [`ServiceOrderDto`](#serviceorderdto).

#### `PUT /service-orders/{id}/estimate`

- Corpo: [`AddServiceOrderEstimateCommand`](#addserviceorderestimatecommand) (o `serviceOrderId` vem da rota).
- `200`: envelope com [`ServiceOrderDto`](#serviceorderdto); a OS passa a "aguardando resposta".
- Efeitos: WhatsApp com o orçamento e o código de segurança (exceto empresa Philco/Britania).

#### `PUT /service-orders/{id}/estimate/approve` e `/estimate/reject`

- Sem corpo. `200`: envelope com [`ServiceOrderDto`](#serviceorderdto).
- Efeitos: notificação interna ("Favor comprar peça..." / "Favor montar a OS ... para entrega") e
  WhatsApp de agradecimento/confirmação ao cliente + aviso ao dono (aprovação de Philco/Britania não
  envia WhatsApp).

#### `PUT /service-orders/{id}/ready-for-pickup`

- Sem corpo. Libera para retirada uma OS com orçamento reprovado.
- Efeitos: WhatsApp avisando que o aparelho pode ser retirado.

#### `PUT /service-orders/{id}/part/purchase`

- Sem corpo. Marca a peça como comprada (data da compra). `200`: [`ServiceOrderDto`](#serviceorderdto).

#### `PUT /service-orders/{id}/part/purchase-cost`

- Corpo: [`UpdatePartPurchaseCostRequest`](#updatepartpurchasecostrequest). `200`: [`ServiceOrderDto`](#serviceorderdto).

#### `PUT /service-orders/{id}/repair`

- Sem corpo. Marca como consertada. Efeitos: WhatsApp "aparelho pronto".

#### `PUT /service-orders/{id}/delivery`

- Sem corpo. Registra a entrega.
- `200`: `application/pdf` (`ordem_servico_{id}.pdf`, via de saída com garantia) quando o orçamento
  foi aprovado e tinha conserto; nos demais casos, envelope de sucesso sem PDF.
- Efeitos: WhatsApp com a garantia (quando há PDF de garantia).

#### `PUT /service-orders/{id}/product/location`

- Corpo: [`AddProductLocationCommand`](#addproductlocationcommand) (o `serviceOrderId` vem da rota).
- `200`: envelope com mensagem de sucesso.

---

### Pagamentos

| Método | Rota | Login | Descrição | Evento |
|--------|------|-------|-----------|--------|
| `GET` | `/service-orders/{id}/payments` | Sim | Lista os pagamentos da OS | — |
| `POST` | `/service-orders/{id}/payments` | Sim | Adiciona pagamento | `payment.added` |
| `PUT` | `/service-orders/{id}/payments/{paymentId}` | Sim | Altera pagamento | `payment.updated` |
| `DELETE` | `/service-orders/{id}/payments/{paymentId}` | Sim | Remove pagamento | `payment.removed` |
| `GET` | `/service-orders/{id}/payments/{paymentId}/receipt` | Sim | Comprovante em PDF | `payment.receipt_printed` |

- `GET .../payments` → envelope com lista de [`PaymentDto`](#paymentdto).
- `POST` → corpo [`AddPaymentCommand`](#addpaymentcommand); `PUT` → corpo
  [`UpdatePaymentCommand`](#updatepaymentcommand); ambos e o `DELETE` devolvem envelope com
  mensagem (`data` texto).
- `GET .../receipt` → `application/pdf` (`comprovante_pagamento_{paymentId}.pdf`), com o valor por extenso.

```bash
curl -X POST http://localhost:5001/service-orders/4312/payments \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"amount":150.00,"paymentDate":"2026-11-06T10:00:00"}'
```

---

### Métricas do dashboard

| Método | Rota | Login | Descrição |
|--------|------|-------|-----------|
| `GET` | `/metrics` | Sim | Indicadores do dashboard por mês ou por período |

Query (todas opcionais):

| Parâmetro | Tipo | Regra |
|-----------|------|-------|
| `month` | int | 1–12; padrão: mês atual |
| `year` | int | 2000–2100; padrão: ano atual |
| `from`, `to` | data | período; **as duas juntas** e `to` ≥ `from`. Quando informadas, `month`/`year` são ignorados |

- `200`: envelope com [`DashboardMetricsDto`](#dashboardmetricsdto).
- `400`: "Informe as duas datas do período." / "A data final não pode ser antes da inicial." / "Mês ou ano inválido."

> Não confundir com `/internal/metrics` (métricas técnicas no formato Prometheus, ver
> [Rotas internas](#rotas-internas)).

---

### Rotas públicas

Usadas pelo site público e pelo link enviado ao cliente; **não exigem login**.

| Método | Rota | Login | Descrição | Tempo real | Evento |
|--------|------|-------|-----------|------------|--------|
| `GET` | `/get-service-order-for-customer/{id}/{code}` | Não | Cliente consulta o orçamento da OS | — | — |
| `PUT` | `/add-service-order-approve-estimate` | Não | Cliente aprova o orçamento | Sim | `estimate.approved_by_customer` |
| `PUT` | `/add-service-order-reject-estimate` | Não | Cliente reprova o orçamento | Sim | `estimate.rejected_by_customer` |
| `POST` | `/v1/whatsapp/site-contact` | Não | Formulário de contato do site → WhatsApp da loja | — | — |

#### `GET /get-service-order-for-customer/{id}/{code}`

- Rota: `id` (número da OS) e `code` (código de segurança enviado no WhatsApp; espaços e
  maiúsculas/minúsculas são ignorados).
- `200`: envelope com [`EstimateServiceOrder`](#estimateserviceorder).
- `404`: "Essa ordem de serviço não existe" — também quando o código não confere (não revela se a OS existe).

#### `PUT /add-service-order-approve-estimate` e `/add-service-order-reject-estimate`

- Corpo: [`CustomerEstimateResponseRequest`](#customerestimateresponserequest).
- Mesmo efeito da aprovação/reprovação feita pela loja (inclusive WhatsApp e notificação interna),
  desde que o código confira.

#### `POST /v1/whatsapp/site-contact`

- `multipart/form-data` com os campos abaixo e anexos opcionais.

| Campo | Obrigatório | Limite |
|-------|-------------|--------|
| `name` | sim | 120 caracteres |
| `phone` | sim | 30 caracteres |
| `email` | não | 200 caracteres |
| `subject` | não (padrão "Contato via site") | 60 caracteres |
| `message` | não | 4000 caracteres |
| arquivos | não | até 10, só `image/*` ou `video/*`, 25 MB no total |

- `200`: `{ "data": "Mensagem enfileirada com sucesso", "code": 200, ... }`
- `400`: "Formato inválido." · "Nome e telefone são obrigatórios." · "Campos excedem o tamanho
  permitido." · "Máximo de 10 arquivos." · "Os anexos excedem 25 MB no total." · "Tipo de arquivo
  não permitido: {arquivo}"
- `500` (sem corpo): fila indisponível.

```bash
curl -X POST http://localhost:5001/v1/whatsapp/site-contact \
  -F name="Maria" -F phone="41999990000" -F subject="Orçamento" \
  -F message="Minha TV não liga" -F "files=@foto.jpg;type=image/jpeg"
```

---

### Tempo real

| Rota | Login | Descrição |
|------|-------|-----------|
| `/osHub` | Não | Hub SignalR que avisa as telas abertas quando algo muda |

- Evento enviado a todos os clientes: **`Atualizar`**, com um texto (a mensagem de sucesso da operação).
- Disparado após sucesso em: criar/editar cliente, criar OS/venda, editar OS, orçamento, aprovar/
  reprovar (loja ou cliente), liberar reprovado, compra de peça, conserto, entrega e localização
  (coluna "Tempo real" das tabelas).
- Ao receber `Atualizar`, a tela recarrega seus dados.

```ts
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/api/osHub')            // local: http://localhost:5001/osHub
  .withAutomaticReconnect()
  .build();
connection.on('Atualizar', (mensagem: string) => recarregar());
await connection.start();
```

### Rotas internas

| Método | Rota | Login | Descrição |
|--------|------|-------|-----------|
| `GET` | `/` | Não | Verificação de saúde: responde `RUNNING` (usado no healthcheck do Docker) |
| `GET` | `/internal/metrics` | Não (só rede interna) | Métricas técnicas no formato Prometheus, coletadas pelo Alloy |
| `POST` | `/internal/whatsapp/test-message` | Não (só rede interna) | Envia uma mensagem **real** de teste para `WHATSAPP_TEST_PHONE` |

#### `POST /internal/whatsapp/test-message`

Envio real de teste, **só sob demanda**, para o número do dono (`WHATSAPP_TEST_PHONE`). Não cria
cliente nem OS. Rode na VPS, na pasta do projeto:

```bash
./scripts/teste-whatsapp.sh
# ou: docker compose exec api curl -s -X POST localhost:8080/internal/whatsapp/test-message
```

| Situação | HTTP | `message` |
|----------|------|-----------|
| Enviada | `200` | "Mensagem de teste enviada para 41*****0000." |
| Evolution recusou ou fora do ar | `502` | "Falha ao enviar a mensagem de teste: …" (motivo) |
| `WHATSAPP_TEST_PHONE` vazio | `400` | "Configure WHATSAPP_TEST_PHONE no .env para usar o envio de teste." |

Texto recebido: "🧪 *Mensagem de teste* do sistema TVS Eletrônica — dd/MM/aaaa HH:mm. Se você
recebeu, o envio de WhatsApp está funcionando." Evento `whatsapp.test`.

As rotas `/internal/*` **não são acessíveis pela internet**: o Caddy responde `404` para
`/api/internal/*`.

---

## Modelos de dados

Todos os nomes de campo em `camelCase`. Tipos: `long`/`int` = inteiro, `decimal` = número com
casas decimais, `DateTime` = data/hora ISO 8601, `?` = pode ser nulo/omitido.

### Objetos de entrada

#### `LoginRequest`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `email` | string | sim | e-mail do usuário |
| `password` | string | sim | senha |

#### `CreateCustomerCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `name` | string | **sim** | nome (gravado em maiúsculas) |
| `phone` | string | **sim** | telefone principal (WhatsApp) |
| `phone2` | string | não | telefone alternativo |
| `email` | string | não | gravado em minúsculas |
| `document` | string? | não | CPF ou CNPJ (validado se informado) |
| `street`, `number`, `neighborhood` | string | não | endereço |
| `city` | string | não | padrão `"Campo Largo"` |
| `state` | string | não | UF, padrão `"PR"` |
| `zipCode` | string | não | CEP |

```json
{
  "name": "Maria da Silva", "phone": "41999990000", "phone2": "", "email": "maria@exemplo.com",
  "document": "12345678909", "street": "Rua XV de Novembro", "number": "100",
  "neighborhood": "Centro", "city": "Campo Largo", "state": "PR", "zipCode": "83601000"
}
```

#### `UpdateCustomerCommand`

Mesmos campos de [`CreateCustomerCommand`](#createcustomercommand) + `id` (long, obrigatório,
diferente de 0). Regras: "O Id do cliente não pode ser 0", nome e telefone obrigatórios.

#### `CreateServiceOrderCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `customerId` | long | **sim** (≠ 0) | cliente |
| `productModel` | string | **sim** | modelo |
| `productSerialNumber` | string | **sim** | número de série |
| `productBrand` | string | não | marca |
| `productDefect` | string | não | defeito relatado |
| `accessories` | string | não | acessórios deixados |
| `productType` | [`EProduct`](#eproduct) | não | padrão `1` (TV) |
| `enterprise` | [`EEnterprise`](#eenterprise) | não | padrão `1` (Particular) |

Textos são gravados em maiúsculas.

#### `CreateSalesServiceOrderCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `customerId` | long | **sim** (≠ 0) | cliente |
| `productModel` | string | **sim** | modelo |
| `productSerialNumber` | string | **sim** | número de série |
| `guarantee` | string | **sim** | garantia ("A venda precisa ter uma garantia") |
| `amount` | decimal | **sim** (≠ 0) | valor da venda |
| `productBrand` | string | não | marca |
| `productType` | [`EProduct`](#eproduct) | não | padrão `1` (TV) |

```json
{ "customerId": 1, "productBrand": "SAMSUNG", "productModel": "UN50", "productSerialNumber": "SN9",
  "guarantee": "90 DIAS", "amount": 1800.00, "productType": 1 }
```

#### `EditServiceOrderCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `model` | string | **sim** | modelo |
| `serialNumber` | string | **sim** | número de série |
| `type` | [`EProduct`](#eproduct) | **sim** | tipo do produto (valor válido) |
| `enterprise` | [`EEnterprise`](#eenterprise) | **sim** | empresa (valor válido) |
| `entryDate` | DateTime | **sim** | data de entrada |
| `brand`, `defect`, `accessories`, `location`, `solution`, `guarantee` | string? | não | textos (gravados em maiúsculas) |
| `inspectionDate`, `responseDate`, `repairDate`, `purchasePartDate`, `deliveryDate` | DateTime? | não | datas das etapas |
| `partCost` | decimal | sim (≥ 0) | valor da peça cobrado |
| `laborCost` | decimal | sim (≥ 0) | mão de obra |
| `estimate` | objeto? | não | dados do orçamento (abaixo). Enviado → substitui todos; omitido → ficam como estão |

`estimate`:

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `serviceOrderStatus` | [`EServiceOrderStatus`](#eserviceorderstatus) | sim | situação ("Situação inválida.") |
| `repairStatus` | [`ERepairStatus`](#erepairstatus) | sim | resposta do orçamento ("Resposta do orçamento inválida.") |
| `repairResult` | [`ERepairResult`](#erepairresult)? | não | resultado; `null` deixa vazio |
| `estimateMessage` | string? | não | mensagem do orçamento; vazio vira `null` |
| `partPurchaseCost` | decimal | sim (≥ 0) | custo de compra da peça |

#### `AddServiceOrderEstimateCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `solution` | string | **sim** | solução proposta |
| `estimateMessage` | string | **sim** | texto do orçamento enviado ao cliente |
| `repairResult` | [`ERepairResult`](#erepairresult) | **sim** | tem conserto / não tem / sem defeito |
| `totalCost` | decimal | sim (≥ 0) | valor do orçamento |
| `warranty` | string | não | garantia |
| `partPurchaseCost` | decimal | não (≥ 0, padrão 0) | custo de compra da peça |
| `serviceOrderId` | long | — | preenchido pela rota |

```json
{ "solution": "Troca da placa fonte", "estimateMessage": "Orçamento de R$ 350,00 com 90 dias de garantia",
  "repairResult": 1, "totalCost": 350.00, "warranty": "90 dias", "partPurchaseCost": 120.00 }
```

#### `UpdatePartPurchaseCostRequest`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `partPurchaseCost` | decimal | sim | novo custo de compra da peça |

#### `AddProductLocationCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `location` | string | **sim** | onde o produto está (ex.: "PRATELEIRA 3"), gravado em maiúsculas |
| `serviceOrderId` | long | — | preenchido pela rota |

#### `AddPaymentCommand`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `amount` | decimal | **sim** (> 0) | "O valor deve ser maior que zero" |
| `paymentDate` | DateTime | **sim** | "Informe a data do pagamento." |

#### `UpdatePaymentCommand`

Mesmos campos e regras de [`AddPaymentCommand`](#addpaymentcommand).

#### `CustomerEstimateResponseRequest`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `id` | long | sim | número da OS |
| `code` | string | sim | código de segurança recebido pelo cliente |

#### `CustomerSearchQuery`

Parâmetros de query de `GET /customers/search` (filtros combinados, todos opcionais):

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `name` | string? | parte do nome (mínimo 3 letras; sem diferenciar maiúsculas e acentos) |
| `phone` | string? | telefone principal ou secundário; só os dígitos contam (mínimo 4) |
| `document` | string? | parte do CPF/CNPJ, com ou sem pontuação (mínimo 3) |
| `pageNumber` | int | padrão `1` (≥ 1) |
| `pageSize` | int | padrão `20` (1 a 100) |

#### `ServiceOrderSearchQuery`

Parâmetros de query de `GET /service-orders/search`. Use **só um** dos filtros (`number`,
`customer`, `model`, `serialNumber` ou o período `from`/`to`):

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `number` | long? | número da OS (> 0) |
| `customer` | string? | nome do cliente (parte, mínimo 3 letras) |
| `model` | string? | modelo |
| `serialNumber` | string? | número de série |
| `from`, `to` | DateTime? | período de entrada; só `from` → até hoje |
| `pageNumber` | int | padrão `1` (≥ 1) |
| `pageSize` | int | padrão `20` (1 a 100) |

### DTOs de saída

#### `AccessTokenDto`

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `accessToken` | string | JWT (1 h) para o cabeçalho `Authorization`; a sessão de 30 dias vai no cookie `tvs_refresh` |

#### `CustomerDto`

`id` (long), `name`, `street`, `neighborhood`, `city`, `number`, `zipCode`, `state`, `phone`,
`phone2` (string), `email` (string?), `document` (string?).

#### `AddressDto`

`zipCode`, `street`, `neighborhood`, `city`, `state` (string).

#### `ServiceOrderDto`

| Grupo | Campos |
|-------|--------|
| OS | `id` (long) |
| Cliente | `customerId`, `name`, `street`, `neighborhood`, `city`, `number`, `zipCode`, `state`, `phone`, `phone2`, `email?`, `document?`, `formattedAddress` (endereço em uma linha) |
| Produto | `brand`, `model`, `serialNumber`, `defect?`, `accessories?`, `type` ([`EProduct`](#eproduct)), `location`, `enterprise` ([`EEnterprise`](#eenterprise)) |
| Datas | `entryDate`, `inspectionDate?`, `responseDate?`, `repairDate?`, `purchasePartDate?`, `deliveryDate?` |
| Orçamento | `solution?`, `estimateMessage?`, `guarantee?` |
| Valores | `partCost`, `laborCost`, `totalAmount` (= peça + mão de obra), `partPurchaseCost` |
| Situação | `serviceOrderStatus` ([`EServiceOrderStatus`](#eserviceorderstatus)), `repairStatus` ([`ERepairStatus`](#erepairstatus)), `repairResult?` ([`ERepairResult`](#erepairresult)) |

#### `ServiceOrderSearchItem`

`number` (long), `customer`, `productType` ([`EProduct`](#eproduct)), `brand`, `model`,
`serialNumber`, `enterprise` ([`EEnterprise`](#eenterprise)), `serviceOrderStatus`
([`EServiceOrderStatus`](#eserviceorderstatus)), `isSale` (bool — venda ou conserto), `enteredAt`.

#### `PaginatedResult`

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `items` | lista | itens da página |
| `totalCount` | int | total de registros |
| `pageNumber` | int | página atual |
| `pageSize` | int | tamanho da página |
| `totalPages` | int? | total de páginas |

#### `PaymentDto`

`id` (long), `amount` (decimal), `paidAt` (DateTime).

#### `EstimateServiceOrder`

O que o cliente vê na página pública do orçamento: `id`, `customerName`, `productType`
([`EProduct`](#eproduct)), `productBrand`, `productModel`, `productDefect`, `guarantee?`,
`totalAmount`, `estimateMessage?`, `status` ([`EServiceOrderStatus`](#eserviceorderstatus)),
`result?` ([`ERepairResult`](#erepairresult)), `repairStatus` ([`ERepairStatus`](#erepairstatus)),
`enterprise` ([`EEnterprise`](#eenterprise)).

#### `DashboardMetricsDto`

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `totalOrders` | int | OS de conserto no período |
| `activeOrders` | int | OS ainda não entregues |
| `totalRevenue` | decimal | faturamento dos consertos entregues |
| `approvalRate` | double | % de aprovados entre os orçamentos já respondidos |
| `averageCycleDays` | double | dias médios da entrada à entrega (consertos entregues) |
| `totalSales` | int | quantidade de vendas |
| `totalSalesRevenue` | decimal | faturamento das vendas entregues |
| `pendingDeliveryRevenue` | decimal | valor de consertos aprovados (com conserto) ainda não entregues |
| `totalPartsSpend` | decimal | custo de compra das peças das OS entregues |
| `totalValue` | decimal | `totalRevenue` + `totalSalesRevenue` |
| `netTotal` | decimal | `totalValue` − `totalPartsSpend` |
| `deliveredProducts` | lista de [`DeliveredProductDto`](#deliveredproductdto) | produtos entregues no período |
| `ordersByStatusLabels` / `ordersByStatusValues` | lista de texto / número | gráfico por situação |
| `repairsByProductTypeLabels` / `...Values` | idem | consertos por tipo de produto |
| `salesByProductTypeLabels` / `...Values` | idem | vendas por tipo de produto |
| `repairsByEnterpriseLabels` / `...Values` | idem | consertos por empresa |
| `salesByEnterpriseLabels` / `...Values` | idem | vendas por empresa |
| `repairResultLabels` / `repairResultValues` | idem | resultados dos orçamentos |

#### `DeliveredProductDto`

`id`, `customerName`, `brand`, `model`, `productLabel`, `typeLabel`, `statusLabel` ("Reparo" ou
"Venda"), `enterpriseLabel`, `deliveryDate`, `totalAmount`, `partPurchaseCost`.

### Enumerações

Enviadas e recebidas como **números**.

#### `EProduct`

| Valor | Código | Significado |
|-------|--------|-------------|
| `1` | `Tv` | TV |
| `2` | `TvBox` | TV-Box |
| `5` | `ControleRemoto` | Controle remoto |
| `6` | `Som` | Som |
| `10` | `CaixaAcustica` | Caixa acústica |
| `18` | `Microondas` | Micro-ondas |
| `19` | `Fonte` | Fonte |
| `20` | `Bases` | Bases |
| `21` | `PlacaFonte` | Placa fonte |
| `22` | `PlacaPrincipal` | Placa principal |

#### `EEnterprise`

| Valor | Código | Significado |
|-------|--------|-------------|
| `1` | `Particular` | Cliente particular |
| `3` | `Cocel` | Cocel |
| `8` | `Seguradora` | Seguradora |
| `16` | `PhilcoBritania` | Philco/Britania (garantia de fábrica; não recebe WhatsApp de orçamento/aprovação) |
| `20` | `Copel` | Copel |

#### `EServiceOrderStatus`

| Valor | Código | Significado |
|-------|--------|-------------|
| `1` | `Entered` | Entrada |
| `2` | `Evaluated` | Avaliado (orçamento feito) |
| `3` | `Repaired` | Consertado |
| `4` | `Delivered` | Entregue |
| `5` | `OrderPart` | Aguardando peça |
| `6` | `ReadyToPickUp` | Pronto para entrega |

#### `ERepairStatus`

| Valor | Código | Significado |
|-------|--------|-------------|
| `1` | `Entered` | Orçamento pendente |
| `2` | `Approved` | Aprovado |
| `3` | `Disapproved` | Reprovado |
| `4` | `Waiting` | Aguardando resposta |

#### `ERepairResult`

| Valor | Código | Significado |
|-------|--------|-------------|
| `1` | `Repair` | Tem conserto |
| `2` | `Unrepaired` | Não tem conserto |
| `3` | `NoDefectFound` | Não apresentou defeito |

#### `EDocumentType`

| Valor | Código | Significado |
|-------|--------|-------------|
| `1` | `Cpf` | CPF |
| `2` | `Cnpj` | CNPJ |

Usado internamente na validação do documento do cliente (o campo `document` é texto).

---

## Notificações de WhatsApp

As mensagens são colocadas numa **fila em memória** e enviadas em segundo plano pela Evolution API,
para não atrasar a resposta da API. O resultado de cada envio aparece nos logs e no painel
**WhatsApp** do Grafana.

| Mensagem | Para | Quando |
|----------|------|--------|
| `SendWelcomeMessageRequest` | cliente | OS de conserto criada |
| `SendProductSaleMessageRequest` | cliente | venda registrada |
| `SendEstimateMessageRequest` | cliente | orçamento registrado (com código de segurança e link) — exceto Philco/Britania |
| `SendThankYouForEstimateApprovalRequest` | cliente | orçamento aprovado (loja ou cliente) — exceto Philco/Britania |
| `SendEstimateDisapprovalAcknowledgmentRequest` | cliente | orçamento reprovado |
| `NotifyOwnerEstimateDecisionRequest` | dono da loja | orçamento aprovado ou reprovado |
| `SendRejectionReadyForPickupMessageRequest` | cliente | reprovado liberado para retirada |
| `SendDeviceReadyMessageRequest` | cliente | conserto registrado |
| `SendGuaranteeMessageRequest` | cliente | entrega com garantia |
| `SendSiteContactMessageRequest` | dono da loja | formulário de contato do site (com fotos/vídeos) |

Como conferir se uma mensagem saiu: painel **WhatsApp** no Grafana (enviadas × falhas e motivo) ou
nos logs, `WhatsApp enfileirado` → `WhatsApp enviado` / `WhatsApp falhou` com o número da OS.
Em `Development`, tudo vai para o número de teste.

---

## Docker e produção

O passo a passo completo (domínio, HTTPS, Azure SQL, firewall) está no **[DEPLOY.md](DEPLOY.md)**.
Resumo:

### Serviços (`docker-compose.yml`)

| Serviço | O que é | Portas |
|---------|---------|--------|
| `api` | API .NET (`src/TVSApp.Api/Dockerfile`), porta interna 8080, healthcheck em `/` | — |
| `web` | Caddy: serve o Angular, repassa `/api` para a API, publica o Grafana, emite HTTPS | `HTTP_PORT` (80), `HTTPS_PORT` (443) |
| `loki` | armazena logs (30 dias) | — |
| `prometheus` | armazena métricas (15 dias) | — |
| `alloy` | coleta logs dos containers e métricas | — |
| `grafana` | painéis e alertas, via `/grafana` (e subdomínio opcional) | — |
| `evolution-api`, `evolution-postgres`, `evolution-redis` | WhatsApp (opcional, profile `whatsapp`) — ver [DEPLOY.md](DEPLOY.md#6-whatsapp-evolution-api-na-mesma-vps-opcional) | painel só em `127.0.0.1:8090` |

Volumes: `caddy_data` (certificados — não apague), `caddy_config`, `loki_data`,
`prometheus_data`, `grafana_data` e, com a Evolution, `evolution_instances`, `evolution_postgres`,
`evolution_redis` (sessão do WhatsApp — não apague).

### Comandos

```bash
cp .env.example .env && nano .env      # preencha (tabela abaixo)
docker compose up -d --build           # sobe tudo
docker compose ps                      # estado dos serviços
docker compose logs -f api             # logs da API
docker compose logs -f web             # logs do Caddy
docker compose restart api             # reinicia a API

git pull && docker compose up -d --build   # atualizar para uma versão nova
docker image prune -f                      # opcional: limpa imagens antigas

docker compose down                    # para tudo (volumes e dados ficam)
```

### Variáveis do `.env`

| Variável | Obrigatória | Para quê |
|----------|-------------|----------|
| `SITE_ADDRESS` | sim | domínio do sistema (HTTPS automático) ou `:80` para HTTP pelo IP |
| `HTTP_PORT`, `HTTPS_PORT` | não | portas da VPS (padrão `80`/`443`); troque se já estiverem em uso, ex.: `8080` → `http://IP:8080` |
| `DB_CONNECTION_STRING` | sim | cadeia ADO.NET do Azure SQL |
| `JWT_SECRET_KEY` | sim | chave dos tokens (`openssl rand -base64 48`) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | não | padrão `tvs-app` |
| `CORS_ORIGINS` | não | sites externos que chamam a API (ex.: o site público) |
| `EVOLUTION_BASE_URL`, `EVOLUTION_API_KEY`, `EVOLUTION_INSTANCE` | para WhatsApp | Evolution API (outro servidor, ou `http://evolution-api:8080` se rodar nesta VPS) |
| `COMPOSE_PROFILES` | não | `whatsapp` sobe também a Evolution API nesta VPS |
| `EVOLUTION_DB_PASSWORD`, `EVOLUTION_MANAGER_PORT` | só com a Evolution nesta VPS | senha do banco interno da Evolution; porta do painel (padrão `8090`) |
| `EVOLUTION_MANAGER_BIND` | não | `127.0.0.1` (padrão, painel só por túnel SSH) ou `0.0.0.0` (painel aberto pelo IP — só enquanto conecta o número) |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | só em banco novo | cria o primeiro usuário |
| `GRAFANA_ADDRESS` | não | subdomínio próprio do Grafana com HTTPS (ex.: `grafana.seudominio.com`); sem ele, use `/grafana` |
| `GRAFANA_ROOT_URL` | não | endereço completo do Grafana para links absolutos (ex.: `http://IP_DA_VPS/grafana/`) |
| `GRAFANA_ADMIN_USER` | não | usuário do Grafana (padrão `admin`) |
| `GRAFANA_ADMIN_PASSWORD` | **sim** | senha do Grafana — sem ela o `docker compose up` não sobe |
| `WHATSAPP_TEST_PHONE` | não | número do dono para o envio real de teste sob demanda (só dígitos com DDD) |
| `ASPNETCORE_ENVIRONMENT` | não | deixe `Production` |

Desenvolvimento com Docker: `docker compose -f docker-compose.dev.yml up` (ver
[Como rodar localmente](#como-rodar-localmente)).

---

## Observabilidade

Sobe junto com o sistema e **não interfere nele**: se a pilha cair, a API segue igual e os logs
continuam em `docker compose logs api`.

```text
api (logs JSON no stdout + /internal/metrics) ─┐
containers e VPS ──────────────────────────────┼─► alloy ─► loki (logs, 30 dias)
                                               │          └► prometheus (métricas, 15 dias)
                                               └──────────────► grafana (painéis e alertas)
```

### Acesso

| Situação | Endereço |
|----------|----------|
| Sem DNS (só o IP da VPS) | `http://IP_DA_VPS/grafana` |
| Com domínio do sistema | `https://SEU_DOMINIO/grafana` |
| Com subdomínio próprio (`GRAFANA_ADDRESS`) | `https://GRAFANA_ADDRESS` |

Login com `GRAFANA_ADMIN_USER` / `GRAFANA_ADMIN_PASSWORD`; sem cadastro aberto e sem acesso
anônimo. Pelo IP a conexão é **HTTP** e a senha trafega sem criptografia — use uma senha exclusiva
para o Grafana. Os painéis ficam na pasta **TVS**:

| Painel | Mostra |
|--------|--------|
| **Visão geral** | API no ar, requisições/min, % de erros 5xx, latência p50/p95 por rota, eventos de negócio por dia, alertas |
| **Logs** | busca livre, filtro por nº da OS e nível, volume por nível |
| **WhatsApp** | conexão do número (conectado/desconectado), enviadas × falhas por dia e por tipo, taxa de falha, últimas falhas com motivo |
| **Servidor** | CPU, memória, disco e rede da VPS; CPU e memória por container |

### Alertas

Aparecem só no Grafana (Alerting → regras, pasta TVS) — **nenhum aviso é enviado**:

| Alerta | Condição |
|--------|----------|
| API fora do ar | API não responde à coleta por 2 min |
| Taxa de erros alta | mais de 5% de respostas 5xx por 5 min |
| WhatsApp falhando | 3 ou mais falhas em 10 min |
| WhatsApp desconectado | número da loja desconectado ou Evolution sem responder por 5 min |
| Disco quase cheio | uso do disco da VPS acima de 85% por 5 min |

### Seguir uma requisição e achar uma OS

- Toda resposta tem o cabeçalho **`X-Correlation-Id`**; todos os logs daquela requisição (acesso,
  evento de negócio, enfileiramento de WhatsApp) levam o mesmo `CorrelationId`.
- **Achar uma OS**: painel **Logs** → campo **Nº da OS** (ex.: `4312`). Aparecem as requisições e
  os eventos daquela OS.
- **Seguir uma requisição**: copie o `CorrelationId` (detalhes da linha de log ou cabeçalho da
  resposta) e cole no campo **Busca**.
- No Explore do Grafana (Loki): `{service="api"} | json | ServiceOrderId="4312"`.

### Campos dos logs

`@t` (horário), `@l` (nível), `@m` (mensagem), `service`, `environment`, `CorrelationId`,
`UserEmail`, `RequestMethod`, `RequestPath` (modelo da rota, ex.: `/service-orders/{id:long}/delivery`),
`StatusCode`, `Elapsed`, `Event`, `Outcome`, `ServiceOrderId`, `CustomerId`, `MessageType`,
`Phone` (mascarado), `Reason`.

### Catálogo de eventos de negócio

Registrados com resultado `success`/`failure` (ver `src/TVSApp.Application/Observability/BusinessEvents.cs`):

| Evento | Quando |
|--------|--------|
| `auth.login` | login |
| `customer.created` / `customer.updated` | cadastro / edição de cliente |
| `service_order.created` / `service_order.updated` | OS aberta / editada |
| `sale.created` | venda registrada |
| `estimate.added` | orçamento registrado |
| `estimate.approved` / `estimate.rejected` | resposta registrada pela loja |
| `estimate.approved_by_customer` / `estimate.rejected_by_customer` | resposta do cliente pelo site |
| `part.purchased` / `part.purchase_cost_updated` | peça comprada / custo alterado |
| `service_order.ready_for_pickup` | reprovado liberado para retirada |
| `service_order.repaired` | conserto registrado |
| `service_order.delivered` | entrega |
| `service_order.location_updated` | localização do produto |
| `service_order.pdf_reprinted` | PDF reimpresso |
| `payment.added` / `payment.updated` / `payment.removed` | pagamentos |
| `payment.receipt_printed` | comprovante gerado |
| `whatsapp.enqueued` / `whatsapp.sent` / `whatsapp.failed` | ciclo de cada mensagem de WhatsApp |
| `whatsapp.test` | envio real de teste sob demanda |

### Métricas

| Métrica | Rótulos | O que mede |
|---------|---------|------------|
| `tvs_business_events_total` | `event`, `outcome` | eventos de negócio |
| `tvs_whatsapp_messages_total` | `type`, `outcome` | mensagens de WhatsApp enviadas/falhas (`type="TestMessage"` no envio de teste) |
| `tvs_whatsapp_connected` | — | `1` conectado / `0` desconectado ou Evolution fora do ar (consultado a cada minuto, sem enviar mensagens); ausente se a Evolution não estiver configurada |
| `http_requests_received_total` | `method`, `endpoint`, `code` | requisições por rota e status |
| `http_request_duration_seconds` | `method`, `endpoint`, `code` | latência (histograma) |
| `up{job="tvs-api"}` | — | API respondendo à coleta |
| `node_*` | — | CPU, memória, disco e rede da VPS |
| `container_*` | `image` | CPU e memória por container |

### Privacidade

Telefones aparecem mascarados (`41*****0000`), e-mails de login com falha também (`m***@loja.com`).
Senhas, tokens e corpos de requisição **nunca** são registrados.

Mais detalhes de operação na seção *Observabilidade* do [DEPLOY.md](DEPLOY.md).

---

## Contribuindo

As regras do projeto estão na constituição ([`.specify/memory/constitution.md`](.specify/memory/constitution.md)). Resumo:

1. **Clean Architecture**: dependências só para dentro (`Api → Infrastructure → Application → Domain`); a Api não tem regra de negócio.
2. **Domain isolado**: sem dependência de outros projetos ou pacotes de infraestrutura; configuração de persistência fica na Infrastructure.
3. **EF Core + migrations**: toda mudança de banco é uma migration versionada; nunca editar migration já em `main`.
4. **Regras de negócio testadas**: regra nova ou alterada vem com teste em `TVSApp.Tests` no mesmo PR.
5. **Interface em português**: todo texto visto pelo usuário (telas, mensagens da API, WhatsApp) em pt-BR.

### Migrations

```bash
dotnet tool install --global dotnet-ef   # uma vez
dotnet ef migrations add NomeDescritivo --project src/TVSApp.Infrastructure --startup-project src/TVSApp.Api
```

A API aplica as migrations pendentes ao subir.

### Antes de abrir um PR

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
cd web && npm run build    # se mexeu no frontend
```

### Fluxo de features (Spec Kit)

Cada feature tem especificação, plano e tarefas em `specs/NNN-nome/`:

```text
/speckit-specify  →  /speckit-plan  →  /speckit-tasks  →  /speckit-implement
   spec.md            plan.md, research.md,        tasks.md        código
                      data-model.md, contracts/
```

---

## Licença

[MIT](LICENSE) © 2025 AlysonRyan01
