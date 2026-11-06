# Tasks: README completo do projeto (pt-BR)

**Input**: Design documents from `/specs/017-readme-docs/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/readme-structure.md, quickstart.md

**Tests**: sem testes automatizados de código (nada no sistema muda); a validação é o roteiro do quickstart.md (rotas código × README, variáveis, comandos, segredos).

**Organization**: US1 entender e rodar · US2 rotas e formatos da API · US3 Docker/produção · US4 observabilidade · US5 contribuição.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente). Quase tudo edita o mesmo `README.md`, então as edições são sequenciais.

---

## Phase 1: Setup

- [X] T001 Conferir a base sem alterações (`dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` com 202 verdes) e ler o `README.md` atual (só `# TVS-App`), `DEPLOY.md`, `.env.example`, `docker-compose.yml`, `docker-compose.dev.yml` e `.specify/memory/constitution.md`
- [X] T002 [P] Gerar o inventário de rotas do código (41 `Map*` em `src/TVSApp.Api/Endpoints/**`, mais `GET /` e `MapMetrics("/internal/metrics")` em `src/TVSApp.Api/Program.cs`/`Common/ObservabilityExtensions.cs` e `MapHub("/osHub")` em `src/TVSApp.Api/Common/AppExtensions.cs`), anotando para cada uma: método, caminho, `RequireAuthorization` ou não, parâmetros de rota/query, tipo do corpo, tipo de retorno (envelope, `Results.File` PDF, cabeçalho `X-Service-Order-Id`) e `.WithBusinessEvent(...)` — rascunho fora do repositório (scratchpad)
- [X] T003 [P] Gerar o inventário de modelos: records de `src/TVSApp.Application/Requests/**` (exceto `WhatsAppQueueRequests`), corpos inline nos endpoints (ex.: custo da peça, entrega, cadastro em `/auth/register`), DTOs de `src/TVSApp.Application/DTOs/**`, enums de `src/TVSApp.Domain/Enums/*` com valores numéricos, forma de pagamento (enum/constante em `src/TVSApp.Domain`), paginação (se houver) e validações (`Validate()`) de cada comando — rascunho no scratchpad
- [X] T004 [P] Confirmar detalhes abertos do research: se `isSuccess` aparece no JSON do envelope `BaseResponse` (`src/TVSApp.Domain/Shared/BaseResponse.cs`), códigos do `src/TVSApp.Api/Middlewares/ExceptionMiddleware.cs`, porta real do `docker-compose.dev.yml` (`5001:80`) e do `launchSettings.json` (`http://localhost:5001`), `apiUrl` de `web/src/environments/*`, Swagger só em `Development`, evento enviado pelo hub (`"Atualizar"`)

---

## Phase 2: Foundational

- [X] T005 Escrever em `README.md` o esqueleto com as 15 seções do contrato (`contracts/readme-structure.md`), na ordem: TVS-App, Índice, Sobre o projeto, Tecnologias, Arquitetura, Estrutura de pastas, Como rodar localmente, Testes, API (com subtítulos Autenticação · Clientes e endereço · Ordens de serviço (consultas) · Ordens de serviço (comandos) · Pagamentos · Métricas do dashboard · Rotas públicas · Tempo real · Rotas internas), Modelos de dados, Notificações de WhatsApp, Docker e produção, Observabilidade, Contribuindo, Licença; Índice com links-âncora para todas as seções de nível 2 e para cada grupo de rotas

**Checkpoint**: esqueleto navegável; cada âncora do índice leva à seção.

---

## Phase 3: User Story 1 — Entender e rodar o projeto do zero (P1) 🎯 MVP

**Goal**: pessoa sem contexto entende o produto e roda o sistema e os testes só com o README.

**Independent Test**: quickstart §5 (subir local e rodar testes seguindo só o README).

- [X] T006 [US1] Em `README.md`, seções "TVS-App" (uma frase) e "Sobre o projeto": assistência técnica de TVs/eletrônicos; OS de reparo e venda; orçamento (incluindo aprovação/reprovação pelo cliente pelo site com código de segurança), compra de peça, conserto, liberação, entrega com garantia, localização do produto, pagamentos e comprovantes, PDFs, dashboard de métricas, avisos por WhatsApp, tempo real entre as telas
- [X] T007 [US1] Em `README.md`, "Tecnologias" (.NET 9/ASP.NET Core minimal APIs, EF Core + SQL Server/Azure SQL, Identity + JWT com refresh token, SignalR, QuestPDF, Evolution API, Angular, Caddy, Docker Compose, Serilog/Prometheus/Loki/Grafana/Alloy) e "Arquitetura" com diagrama em bloco de texto (navegador → Caddy → `/api` → API → Azure SQL; fila do WhatsApp em background; SignalR `/osHub`; pilha de observabilidade) e a regra de dependência `Domain ← Application ← Infrastructure ← Api` (Domain sem dependências)
- [X] T008 [US1] Em `README.md`, "Estrutura de pastas": árvore com `src/TVSApp.{Domain,Application,Infrastructure,Api,Tests}`, `web/`, `observability/`, `specs/`, `old-handlers/`, `.specify/`, `docker-compose*.yml`, `DEPLOY.md`, com o papel de cada uma
- [X] T009 [US1] Em `README.md`, "Como rodar localmente": pré-requisitos (.NET 9 SDK, Node 22 + npm, Docker); SQL Server local (`docker compose -f docker-compose.dev.yml up -d sqlserver` ou `docker run` equivalente, avisando que a senha do arquivo é só para desenvolvimento); configuração da API pelas chaves do `appsettings.json` via `dotnet user-secrets` ou variáveis (`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey/Issuer/Audience`, `Cors__Origins`, `EvolutionApiSettings__*`, `Admin__Email/Password`); `dotnet run --project src/TVSApp.Api` em `http://localhost:5001` com migrations aplicadas na subida e Swagger em `/swagger` (só `Development`); web: `cd web && npm ci && npm start` (`http://localhost:4200`, chama `http://localhost:5001`); primeiro usuário via `Admin__*`; observação de que em `Development` o WhatsApp vai para o número de teste
- [X] T010 [US1] Em `README.md`, "Testes": `dotnet test src/TVSApp.Tests` (MSTest) e o que cobrem (regras de domínio/aplicação, cálculo de métricas, pagamentos, mascaramento de logs, catálogo de eventos); build do web `cd web && npm run build`

**Checkpoint**: US1 validável sozinha.

---

## Phase 4: User Story 2 — Consultar rotas e formatos da API (P1)

**Goal**: 100% das rotas e formatos documentados (SC-001, SC-002).

**Independent Test**: quickstart §2 (diff rotas código × README = vazio) e §3.

- [X] T011 [US2] Em `README.md`, "API › Convenções": base `/api` atrás do Caddy (em produção `https://DOMINIO/api/...`; local direto `http://localhost:5001/...`), JSON camelCase, enums como números (link para "Enumerações"), envelope `BaseResponse` (`data`, `message`, `code` e `isSuccess` conforme T004) com exemplos de sucesso e de erro (`data: null`), tabela de códigos HTTP usados (200, 400 validação, 401, 404, 500), cabeçalho `X-Correlation-Id`, rotas que devolvem PDF
- [X] T012 [US2] Em `README.md`, "API › Autenticação": fluxo login → `accessToken` + `refreshToken` → `Authorization: Bearer <accessToken>` → `/auth/refresh` → `/auth/logout`; tabela e blocos de `POST /auth/login`, `POST /auth/logout`, `POST /auth/refresh`, `POST /auth/register` (com login exigido ou não conforme T002), corpos (`LoginRequest`, `RefreshTokenRequest`, corpo de cadastro) e `AuthTokensDto`; mensagem de credenciais inválidas
- [X] T013 [US2] Em `README.md`, "API › Clientes e endereço": `POST /customers`, `PUT /customers/{id}`, `GET /customers` (parâmetros de query conforme T002) e `GET /addresses/{cep}`, com `CreateCustomerCommand` (obrigatórios: `name`, `phone` — "O nome do cliente não pode estar vazio", "O telefone do cliente não pode estar vazio"; padrões `city` "Campo Largo", `state` "PR"), `UpdateCustomerCommand`, `CustomerDto`, `CustomerSelectDto`, `AddressDto` e eventos `customer.created`/`customer.updated`
- [X] T014 [US2] Em `README.md`, "API › Ordens de serviço (consultas)": `GET /service-orders/pending-estimate`, `/awaiting-response`, `/approved`, `/rejected`, `/awaiting-part`, `/ready-for-pickup`, `/search` (`ServiceOrderSearchQuery`), `/{id}`, `/{id}/pdf` (PDF, evento `service_order.pdf_reprinted`) com paginação/filtros conforme T002/T003 e `ServiceOrderDto`
- [X] T015 [US2] Em `README.md`, "API › Ordens de serviço (comandos)": `POST /service-orders` (`CreateServiceOrderCommand` — `customerId` ≠ 0, `productModel` e `productSerialNumber` obrigatórios, `productType` padrão `Tv`, `enterprise` padrão `Particular`; resposta PDF + `X-Service-Order-Id`), `POST /service-orders/sales` (`CreateSalesServiceOrderCommand`, PDF + cabeçalho), `PUT /{id}` (`EditServiceOrderCommand`), `/{id}/estimate` (`AddServiceOrderEstimateCommand`), `/{id}/estimate/approve`, `/{id}/estimate/reject`, `/{id}/ready-for-pickup`, `/{id}/part/purchase`, `/{id}/part/purchase-cost` (corpo com `partPurchaseCost`), `/{id}/repair`, `/{id}/delivery` (PDF), `/{id}/product/location` (`AddProductLocationCommand`), cada uma com o evento de negócio e se envia WhatsApp e/ou aviso em tempo real
- [X] T016 [US2] Em `README.md`, "API › Pagamentos": `GET /service-orders/{id}/payments`, `POST /service-orders/{id}/payments` (`AddPaymentCommand`), `PUT /service-orders/{id}/payments/{paymentId}` (`UpdatePaymentCommand`), `DELETE /service-orders/{id}/payments/{paymentId}`, `GET /service-orders/{id}/payments/{paymentId}/receipt` (PDF), com `PaymentDto`, formas de pagamento e eventos `payment.*`
- [X] T017 [US2] Em `README.md`, "API › Métricas do dashboard" (`GET /metrics` com `month`, `year`, `from`, `to` — regras de precedência conforme `MetricsQueryHandler`; `DashboardMetricsDto`, `DeliveredProductDto`), "Rotas públicas" (`GET /get-service-order-for-customer/{id}/{code}`, `PUT /add-service-order-approve-estimate`, `PUT /add-service-order-reject-estimate` com `CustomerEstimateResponseRequest`, `POST /v1/whatsapp/site-contact` com campos/anexos de `SendSiteContactMessageRequest`; eventos `estimate.*_by_customer`), "Tempo real" (`/osHub`, evento `"Atualizar"` com a mensagem, quando é disparado, conexão via SignalR) e "Rotas internas" (`GET /` → `RUNNING`, `GET /internal/metrics` só na rede Docker, `/api/internal/*` → 404)
- [X] T018 [US2] Em `README.md`, "Modelos de dados": tabelas campo/tipo/obrigatório/descrição + exemplo JSON camelCase de todos os objetos de entrada e DTOs referenciados em T012–T017 (sem repetir: as rotas linkam para cá) e "Enumerações": `EDocumentType`, `EEnterprise`, `EProduct`, `ERepairResult`, `ERepairStatus`, `EServiceOrderStatus` (+ forma de pagamento) com valor numérico, nome no código e significado em pt-BR
- [X] T019 [US2] Rodar quickstart §2 (diff rotas código × README) e §3 (todos os requests/DTOs/enums citados com todos os campos/valores); corrigir `README.md` até zerar diferenças

**Checkpoint**: US1 + US2 = MVP da documentação.

---

## Phase 5: User Story 3 — Subir em produção com Docker (P2)

**Goal**: comandos e variáveis de produção no README, detalhe no `DEPLOY.md`.

**Independent Test**: quickstart §4 (variáveis) e `docker compose config --quiet`.

- [X] T020 [US3] Em `README.md`, "Docker e produção": tabela de serviços do `docker-compose.yml` (`api`, `web`/Caddy com 80/443, `loki`, `prometheus`, `alloy`, `grafana`) e volumes; comandos `cp .env.example .env`, `docker compose up -d --build`, `docker compose ps`, `docker compose logs -f api`, `docker compose restart api`, atualização (`git pull && docker compose up -d --build`, `docker image prune -f`), `docker compose down`; tabela de **todas** as variáveis do `.env.example` (`SITE_ADDRESS`, `DB_CONNECTION_STRING`, `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`, `CORS_ORIGINS`, `EVOLUTION_BASE_URL`, `EVOLUTION_API_KEY`, `EVOLUTION_INSTANCE`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `GRAFANA_ADDRESS`, `GRAFANA_ADMIN_USER`, `GRAFANA_ADMIN_PASSWORD`, `ASPNETCORE_ENVIRONMENT`) com obrigatoriedade e finalidade; link para `DEPLOY.md` (domínio, HTTPS, Azure SQL); ambiente de desenvolvimento com `docker-compose.dev.yml`

---

## Phase 6: User Story 4 — Usar a observabilidade (P2)

**Goal**: quem investiga sabe onde e como olhar.

**Independent Test**: com o README, achar uma OS no painel Logs.

- [X] T021 [US4] Em `README.md`, "Observabilidade": componentes (Alloy → Loki/Prometheus → Grafana), acesso `https://GRAFANA_ADDRESS`, painéis (Visão geral, Logs, WhatsApp, Servidor), 4 alertas (API fora do ar 2 min, erros 5xx > 5% por 5 min, WhatsApp 3+ falhas em 10 min, disco > 85% por 5 min — sem envio), retenção (logs 30 dias, métricas 15 dias), `X-Correlation-Id`, como achar uma OS (campo "Nº da OS") e seguir uma requisição (CorrelationId na busca), catálogo dos 25 eventos de `src/TVSApp.Application/Observability/BusinessEvents.cs`, métricas próprias (`tvs_business_events_total{event,outcome}`, `tvs_whatsapp_messages_total{type,outcome}`, `http_requests_received_total`, `http_request_duration_seconds`, `up{job="tvs-api"}`, `node_*`, `container_*`), privacidade (telefone `41*****0000`, e-mail mascarado, sem senha/token/corpo) e que o sistema não depende da pilha; link para a seção do `DEPLOY.md`
- [X] T022 [US4] Em `README.md`, "Notificações de WhatsApp": tabela das mensagens (`SendWelcomeMessageRequest`, `SendEstimateMessageRequest`, `SendDeviceReadyMessageRequest`, `SendGuaranteeMessageRequest`, `SendProductSaleMessageRequest`, `SendRejectionReadyForPickupMessageRequest`, `NotifyOwnerEstimateDecisionRequest`, `SendThankYouForEstimateApprovalRequest`, `SendEstimateDisapprovalAcknowledgmentRequest`, `SendSiteContactMessageRequest`) com quando cada uma é disparada (conforme os handlers em `src/TVSApp.Application`), fila em background via Evolution API, e como conferir envio (painel WhatsApp / logs `WhatsApp enviado|falhou`)

---

## Phase 7: User Story 5 — Contribuir (P3)

**Goal**: regras do projeto acessíveis.

**Independent Test**: achar no README como criar migration e onde ficam as specs.

- [X] T023 [US5] Em `README.md`, "Contribuindo": resumo dos 5 princípios da constituição (camadas, Domain isolado, EF Core + migrations, regras testadas, pt-BR) com link para `.specify/memory/constitution.md`; criar migration (`dotnet ef migrations add NomeDescritivo --project src/TVSApp.Infrastructure --startup-project src/TVSApp.Api`); quality gates (`dotnet build`, `dotnet test`, build do web); fluxo Spec Kit (`/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`, artefatos em `specs/NNN-*`); e "Licença" apontando para `LICENSE`

---

## Phase 8: Polish & validação

- [X] T024 Rodar o quickstart completo: §2 rotas (diff vazio), §4 variáveis (nenhuma faltando), §6 segredos (`grep -iE 'password=|apikey|secret' README.md` só placeholders) e links internos (toda âncora do índice existe; arquivos linkados existem)
- [X] T025 Executar os comandos do README: SQL Server local + `dotnet run --project src/TVSApp.Api` (responde `RUNNING` em `/` e Swagger em `/swagger`), `cd web && npm ci && npm run build`, `dotnet test src/TVSApp.Tests`, `docker compose config --quiet` com `.env` de exemplo; corrigir o README onde algo divergir
- [X] T026 Revisão final de `README.md`: tudo em pt-BR, títulos consistentes, sem duplicar o `DEPLOY.md` inteiro, tamanho razoável (se passar de ~2.000 linhas, avaliar mover a referência para `docs/` conforme research R1)

---

## Dependencies & Execution Order

- T001 → T002 ∥ T003 ∥ T004 → T005 (esqueleto) → histórias.
- US1 (T006–T010) e US2 (T011–T019) dependem de T005; US2 usa os inventários T002–T004.
- US3 (T020), US4 (T021–T022) e US5 (T023) dependem só de T005.
- Todas as edições são no mesmo `README.md` → executar sequencialmente.
- Polish (T024–T026) por último.

### Parallel Opportunities

- T002 ∥ T003 ∥ T004 (inventários em rascunhos separados, leitura apenas).

### Parallel Example

```text
T002 inventário de rotas  |  T003 inventário de modelos/enums  |  T004 detalhes de serialização e portas
```

## Implementation Strategy

1. **MVP**: Setup + esqueleto + US1 + US2 → README que explica o projeto e documenta 100% da API.
2. **US3**: Docker/produção.
3. **US4**: observabilidade e WhatsApp.
4. **US5**: contribuição.
5. Validação completa do quickstart.

## Notas da implementação

- O código tem **39** rotas `Map*` (não 41 como estimado no plano); o README documenta as 39 + `GET /` e `GET /internal/metrics` (diff do quickstart §2: só essas duas a mais, esperado).
- Descobertas documentadas a partir do código: busca aceita **um filtro por vez** (`pageSize` 1–100); login inválido = HTTP 400 com `code: 401`; refresh inválido = HTTP 404 com `code: 401`; `/auth/register` exige login; código errado na consulta pública = 404 "Essa ordem de serviço não existe"; `isSuccess` é serializado no envelope; CORS de desenvolvimento precisa incluir `http://localhost:4200`.
- Validado com a API rodando localmente (SQL Server do `docker-compose.dev.yml`): `RUNNING`, Swagger, login, criar cliente/OS (PDF + `X-Service-Order-Id`), orçamento, pagamento, venda, contato do site, busca, métricas e CORS; `npm run build` do web; `dotnet test` 202 verdes.
