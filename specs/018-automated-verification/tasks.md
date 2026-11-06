# Tasks: Verificação automática do sistema (fluxos e WhatsApp)

**Input**: Design documents from `/specs/018-automated-verification/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: incluídos — a verificação de fluxo (US1) **é** um conjunto de testes; lógica nova de US2/US3 tem testes unitários (Constituição IV).

**Organization**: US1 fluxo completo sem envio real · US2 monitor de conexão · US3 envio real de teste sob demanda.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` (202 verdes); Docker rodando e imagem `mcr.microsoft.com/mssql/server:2022-latest` disponível
- [X] T002 Criar o projeto `src/TVSApp.IntegrationTests/TVSApp.IntegrationTests.csproj` (MSTest, `net9.0`, `IsPackable=false`, `Nullable`/`ImplicitUsings` como em `src/TVSApp.Tests/TVSApp.Tests.csproj`) com pacotes `MSTest`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing` 9.x e `Testcontainers.MsSql`; `ProjectReference` para `src/TVSApp.Api`, `src/TVSApp.Application`, `src/TVSApp.Infrastructure`; `MSTestSettings.cs` com `[assembly: DoNotParallelize]`; adicionar ao `TVSApp.sln` (`dotnet sln add`)
- [X] T003 [P] Adicionar `ProjectReference` para `src/TVSApp.Infrastructure` em `src/TVSApp.Tests/TVSApp.Tests.csproj` (testes unitários do `WhatsappService` e do monitor)

---

## Phase 2: Foundational

- [X] T004 Expor o `Program` para testes: acrescentar `public partial class Program;` ao final de `src/TVSApp.Api/Program.cs` (sem outra mudança de comportamento)

**Checkpoint**: `dotnet build TVSApp.sln` compila com o projeto novo.

---

## Phase 3: User Story 1 — Fluxo completo verificado sem WhatsApp real (P1) 🎯 MVP

**Goal**: um comando percorre o ciclo da OS/venda pela API real e confere cada mensagem de WhatsApp, sem enviar nada nem tocar no banco de produção.

**Independent Test**: `dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"` → todos os cenários aprovados em ≤ 5 min; quebrar o envio de "aparelho pronto" → o cenário de conserto falha apontando a etapa (quickstart §2).

### Infraestrutura da verificação

- [X] T005 [P] [US1] Criar `src/TVSApp.IntegrationTests/Infrastructure/SqlServerFixture.cs`: `[AssemblyInitialize]` sobe um `MsSqlContainer` (Testcontainers, imagem `mcr.microsoft.com/mssql/server:2022-latest`) e expõe a connection string com `Database=TvsVerificacao`; `[AssemblyCleanup]` descarta o container
- [X] T006 [P] [US1] Criar `src/TVSApp.IntegrationTests/Infrastructure/FakeEvolutionHandler.cs` (`HttpMessageHandler`): registra cada requisição como `CapturedMessage { Kind ("text" para `/message/sendText/`, "media" para `/message/sendMedia/`), Instance (último segmento da URL), Number (campo `number` do JSON), Text (campo `text`, ou `caption` na mídia), ReceivedAt }` numa lista thread-safe; responde `200` com `{}`; métodos `Clear()`, `WaitForAsync(Func<CapturedMessage,bool> match, TimeSpan timeout = 10s)` (retorna a mensagem ou `null`) e `AssertNoneAsync(Func<CapturedMessage,bool> match, TimeSpan wait = 2s)`
- [X] T007 [US1] Criar `src/TVSApp.IntegrationTests/Infrastructure/ApiFactory.cs` (`WebApplicationFactory<Program>`): ambiente `Production`; `ConfigureAppConfiguration` com `ConnectionStrings:DefaultConnection` do T005, `Jwt:SecretKey/Issuer/Audience` de teste, `Admin:Email=verificacao@tvs.local`, `Admin:Password=Verificacao@123`, `Cors:Origins=http://localhost`, `EvolutionApiSettings:BaseUrl=http://evolution.invalid` (segunda camada de proteção: nenhum envio real possível), `EvolutionApiSettings:Instance=tvs-teste`, `EvolutionApiSettings:ApiKey=chave-teste`; `ConfigureTestServices` reconfigura `services.AddHttpClient<IWhatsappService, WhatsappService>().ConfigurePrimaryHttpMessageHandler(() => Fake)` com uma instância única de `FakeEvolutionHandler` exposta em `Fake`; instância compartilhada por toda a execução
- [X] T008 [US1] Criar `src/TVSApp.IntegrationTests/Infrastructure/ApiClient.cs`: login com o usuário inicial (guarda o Bearer), helpers `CreateCustomerAsync(name, phone)` (telefones fictícios `419000000NN`, nome único por cenário), `CreateServiceOrderAsync(customerId, enterprise = 1)` (lê `X-Service-Order-Id`, confere `application/pdf`), `AddEstimateAsync(id, repairResult = 1, totalCost = 350)`, `GetServiceOrderAsync(id)`, `GetSecurityCodeAsync(id)` (lido direto do banco via `ApplicationDataContext` do `ApiFactory.Services`, só para o cenário do link público) e `PutAsync/PostAsync/GetAsync` que desserializam o envelope `{ data, message, code, isSuccess }` em camelCase; asserts com mensagem pt-BR `"Etapa {etapa}: esperado {x} / obtido {y}"`
- [X] T009 [US1] Criar `src/TVSApp.IntegrationTests/Infrastructure/VerificationTestBase.cs`: classe base dos cenários (acesso ao `ApiFactory`/`ApiClient`/`Fake`, `[TestInitialize]` chama `Fake.Clear()`), helper `ExpectWhatsAppAsync(etapa, phoneDigits, params string[] trechos)` que aguarda a mensagem para o número formatado (`55` + DDD + número) contendo todos os trechos, e `ExpectNoWhatsAppAsync(etapa, phoneDigits)`

### Cenários (contracts/verification-command.md)

- [X] T010 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/AutenticacaoTests.cs`: `[TestMethod("Login com usuário inicial devolve tokens")]` e `[TestMethod("Login com senha errada é recusado")]` (HTTP 400, `code` 401)
- [X] T011 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/OrdemDeServicoTests.cs`: "Abrir OS gera PDF, número e WhatsApp de boas-vindas" (PDF, `X-Service-Order-Id`, mensagem com "bem-vindo" e o nº da OS para o telefone do cliente)
- [X] T012 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/OrcamentoTests.cs`: "Orçamento envia WhatsApp com número e código de segurança"; "Aprovação pela loja envia agradecimento ao cliente e aviso ao dono" (mensagem ao cliente com o nº da OS + mensagem ao dono contendo "aprovou" e o nº); "Reprovação envia confirmação ao cliente e aviso ao dono"; "Liberar reprovado envia aviso de retirada"
- [X] T013 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/PhilcoBritaniaTests.cs`: "Philco/Britania não recebe WhatsApp de orçamento nem de aprovação" (OS com `enterprise: 16`; `ExpectNoWhatsAppAsync` após orçamento e após aprovação)
- [X] T014 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/PublicoTests.cs`: "Cliente consulta e aprova pelo link com o código" (`GET /get-service-order-for-customer/{id}/{code}` 200, `PUT /add-service-order-approve-estimate` 200, mesmas mensagens da aprovação pela loja); "Código errado é recusado sem enviar mensagem" (404 e nenhuma mensagem); "Contato do site envia ao dono" (`POST /v1/whatsapp/site-contact` multipart com `name`/`phone`, mensagem contendo o nome e o telefone do visitante)
- [X] T015 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/ConsertoEEntregaTests.cs`: "Compra de peça e conserto enviam aparelho pronto" (`/part/purchase` 200, `/repair` 200, mensagem de aparelho pronto com nº); "Entrega de OS aprovada com conserto devolve PDF e envia garantia"
- [X] T016 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/PagamentosTests.cs`: "Pagamentos: adicionar, alterar, listar, comprovante e remover" (POST/PUT/GET/DELETE em `/service-orders/{id}/payments`, comprovante `application/pdf`; validação "O valor deve ser maior que zero" com `amount: 0` → 400)
- [X] T017 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/VendaTests.cs`: "Venda gera PDF e envia WhatsApp de venda" (`POST /service-orders/sales`, PDF + `X-Service-Order-Id`, mensagem para o cliente)
- [X] T018 [US1] Em `src/TVSApp.IntegrationTests/Fluxos/ConsultasTests.cs`: "Busca encontra a OS criada pelo cliente" (`/service-orders/search?customer=<nome>`), "Listas por etapa e detalhe respondem" (`/service-orders/pending-estimate`, `/service-orders/{id}`) e "Métricas do mês respondem" (`GET /metrics` 200)
- [X] T019 [US1] Rodar `dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"` 3 vezes seguidas (SC-006) medindo o tempo (≤ 5 min, SC-001); prova de detecção (SC-003): comentar temporariamente o `Enqueue(new SendDeviceReadyMessageRequest(...))` em `src/TVSApp.Application/Handlers/ServiceOrderCommandHandler.cs`, confirmar a falha com a etapa identificada e desfazer

**Checkpoint**: MVP — o teste manual com cliente falso deixa de ser necessário.

---

## Phase 4: User Story 2 — Saber se o WhatsApp da loja está conectado (P2)

**Goal**: estado da conexão no painel e alerta quando cair, sem enviar mensagens.

**Independent Test**: quickstart §3 (Evolution local com instância não conectada → `tvs_whatsapp_connected 0`, log de queda, alerta em ≤ 10 min).

- [X] T020 [P] [US2] Testes em `src/TVSApp.Tests/Infrastructure/WhatsAppConnectionStateTests.cs` (handler HTTP falso): resposta `{"instance":{"state":"open"}}` → conectado; `"connecting"` e `"close"` → desconectado com motivo `instância em "<state>"`; HTTP 401/404/500, timeout e exceção → indisponível com motivo; `BaseUrl` vazio → "não configurado"
- [X] T021 [US2] Em `src/TVSApp.Application/Interfaces/Services/IWhatsappService.cs` adicionar `Task<WhatsAppConnectionStatus> GetConnectionStateAsync(CancellationToken ct)` e criar `src/TVSApp.Application/DTOs/WhatsAppConnectionStatus.cs` (`record WhatsAppConnectionStatus(WhatsAppConnectionState State, string? Reason)` + enum `WhatsAppConnectionState { NotConfigured, Connected, Disconnected, Unavailable }`)
- [X] T022 [US2] Implementar `GetConnectionStateAsync` em `src/TVSApp.Infrastructure/Services/Whatsapp/WhatsappService.cs`: `GET {BaseUrl}/instance/connectionState/{Instance}` com cabeçalho `apikey`, timeout de 10 s, mapeamento do data-model ("open" → Connected; "connecting"/"close" → Disconnected; erro/timeout/não-sucesso → Unavailable; `BaseUrl` vazio → NotConfigured); T020 deve passar
- [X] T023 [US2] Criar `src/TVSApp.Infrastructure/Configurations/WhatsAppMonitorSettings.cs` (`IntervalSeconds = 60`) e `src/TVSApp.Infrastructure/Services/Whatsapp/WhatsAppConnectionMonitor.cs` (`BackgroundService`): a cada intervalo resolve `IWhatsappService` num escopo, chama `GetConnectionStateAsync`; guarda o último estado; publica `ObservableGauge<int>` `whatsapp_connected` no `Meter(BusinessEvents.MeterName)` (1 se Connected, 0 se Disconnected/Unavailable, sem medição se NotConfigured → `tvs_whatsapp_connected`); loga **só na mudança** com escopo `Event = whatsapp.connection` (`Warning` "WhatsApp conexão: desconectado ({Reason:l})" / "indisponível ({Reason:l})", `Information` "WhatsApp conexão: conectado"); nunca envia mensagem; exceções não derrubam o serviço
- [X] T024 [US2] Registrar em `src/TVSApp.Api/Common/BuilderExtensions.cs`: `Configure<WhatsAppMonitorSettings>(configuration.GetSection("WhatsAppMonitor"))` e `AddHostedService<WhatsAppConnectionMonitor>()`; desligar o monitor no `ApiFactory` dos testes de integração (remover o hosted service em `ConfigureTestServices` de `src/TVSApp.IntegrationTests/Infrastructure/ApiFactory.cs`)
- [X] T025 [P] [US2] Grafana: em `observability/grafana/dashboards/whatsapp.json` adicionar no topo o stat "Conexão" (`max(tvs_whatsapp_connected)`, mapeamento 1 → "Conectado" verde, 0 → "Desconectado" vermelho, sem dados → "Não monitorado") e a série "Estado da conexão" (state timeline); em `observability/grafana/provisioning/alerting/rules.yaml` adicionar a regra `uid: tvs-whatsapp-desconectado`, título "WhatsApp desconectado", `max(tvs_whatsapp_connected) < 1` por `5m`, `noDataState: OK`, `severity: critical`, resumo pt-BR
- [X] T026 [US2] Validar quickstart §3 com a Evolution do compose (profile `whatsapp`, instância criada sem conectar): métrica 0, log de queda, parar a Evolution → "indisponível", alerta disparando no Grafana; sem `EVOLUTION_BASE_URL` → sem métrica

---

## Phase 5: User Story 3 — Teste real de envio para o dono (P3)

**Goal**: envio real sob demanda para `WHATSAPP_TEST_PHONE`, sem criar dados.

**Independent Test**: quickstart §4 (sem número → 400; com número → 200 e mensagem no celular; de fora → 404).

- [X] T027 [P] [US3] Testes em `src/TVSApp.Tests/Infrastructure/WhatsAppTestMessageTests.cs` (handler HTTP falso): envia para o número configurado com texto contendo "Mensagem de teste" e "TVS Eletrônica"; Evolution 500 → falha com o motivo; número vazio → recusa "Configure WHATSAPP_TEST_PHONE no .env para usar o envio de teste." sem chamar a Evolution; em `Development` o destino continua sendo o número configurado
- [X] T028 [US3] Em `src/TVSApp.Application/Interfaces/Services/IWhatsappService.cs` adicionar `Task<BaseResponse<string>> SendTestMessageAsync(string? phoneNumber)` e implementar em `src/TVSApp.Infrastructure/Services/Whatsapp/WhatsappService.cs` usando o `Send` existente com o texto do contrato (`🧪 *Mensagem de teste* do sistema TVS Eletrônica — {dd/MM/yyyy HH:mm}. Se você recebeu, o envio de WhatsApp está funcionando.`), retornando 400 sem número e 502 com "Falha ao enviar a mensagem de teste: {motivo}" em erro; T027 deve passar
- [X] T029 [US3] Adicionar `public const string WhatsAppTest = "whatsapp.test";` em `src/TVSApp.Application/Observability/BusinessEvents.cs` (o teste de catálogo existente deve continuar passando)
- [X] T030 [US3] Criar `src/TVSApp.Infrastructure/Configurations/WhatsAppTestSettings.cs` (`Phone`) e `src/TVSApp.Api/Endpoints/Internal/InternalWhatsAppEndpoints.cs` com `POST /internal/whatsapp/test-message` (sem login; lê `WhatsAppTest:Phone`; chama `SendTestMessageAsync`; responde com o envelope e mensagem de sucesso "Mensagem de teste enviada para {LogMask.Phone}."; `.WithBusinessEvent(BusinessEvents.WhatsAppTest)`; incrementa `tvs_whatsapp_messages_total{type="TestMessage",outcome}` e loga `WhatsApp enviado|falhou TestMessage` com telefone mascarado); registrar o mapeamento e `Configure<WhatsAppTestSettings>(GetSection("WhatsAppTest"))` em `src/TVSApp.Api/Common/BuilderExtensions.cs`/`AppExtensions.cs`
- [X] T031 [US3] Em `docker-compose.yml` (serviço `api`) adicionar `WhatsAppTest__Phone: ${WHATSAPP_TEST_PHONE:-}`; em `.env.example` adicionar `WHATSAPP_TEST_PHONE=` com comentário (número do dono para o envio de teste sob demanda, só dígitos com DDD)
- [X] T032 [US3] Cenário de integração em `src/TVSApp.IntegrationTests/Fluxos/MensagemDeTesteTests.cs`: com `WhatsAppTest:Phone` configurado no `ApiFactory` → 200 e mensagem capturada com "Mensagem de teste" para o número; e confirmar que nenhum cliente/OS foi criado (contagem antes/depois)
- [X] T033 [US3] Validar quickstart §4 localmente via Docker: sem número → 400; com número e Evolution falsa/local → resposta coerente; `curl http://localhost:PORTA/api/internal/whatsapp/test-message` pelo Caddy → 404

---

## Phase 6: Polish & validação

- [X] T034 [P] Documentar em `README.md`: seção "Testes" com a verificação de fluxo (`dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"`, exige Docker, o que cobre, que não envia nada) e a estrutura de pastas com `TVSApp.IntegrationTests`; "Rotas internas" com `POST /internal/whatsapp/test-message`; "Observabilidade" com `tvs_whatsapp_connected`, o alerta "WhatsApp desconectado" e o evento `whatsapp.test`; tabela de variáveis com `WHATSAPP_TEST_PHONE`
- [X] T035 [P] Documentar em `DEPLOY.md`: como fazer o envio de teste na VPS (`docker compose exec api curl -s -X POST localhost:8080/internal/whatsapp/test-message`), variável `WHATSAPP_TEST_PHONE`, painel/alerta de conexão
- [X] T036 `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (todos verdes, incluindo os novos) e `dotnet test src/TVSApp.IntegrationTests` verdes; `docker compose config --quiet`; conferir no log da execução que nenhuma requisição saiu para hosts reais (só o handler falso registrou chamadas)

---

## Dependencies & Execution Order

- T001 → T002 ∥ T003 → T004 → histórias.
- **US1**: T005 ∥ T006 → T007 → T008 → T009 → T010…T018 (arquivos diferentes, mas compartilham a base; executar em sequência para depurar) → T019.
- **US2**: T020 → T021 → T022 → T023 → T024 (toca o `ApiFactory` do US1) → T025 ∥ → T026.
- **US3**: T027 → T028 → T029 → T030 → T031 → T032 (usa o `ApiFactory` do US1) → T033.
- US2 e US3 são independentes entre si; ambos tocam `IWhatsappService`/`WhatsappService` → fazer em sequência.
- Polish por último.

### Parallel Opportunities

- T002 ∥ T003; T005 ∥ T006; T020 ∥ T025 (testes × painel); T027 ∥ T025; T034 ∥ T035.

### Parallel Example

```text
US1 base:  T005 SqlServerFixture  |  T006 FakeEvolutionHandler
US2:       T020 testes do estado  |  T025 painel + alerta do Grafana
Polish:    T034 README            |  T035 DEPLOY.md
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 → verificação ponta a ponta com um comando, substituindo o teste manual.
2. **US2**: monitor de conexão + painel/alerta (pega a falha mais comum na prática).
3. **US3**: envio real de teste sob demanda.
4. Docs e validação final.

## Notas da implementação

- Verificação: 25 cenários (22 de fluxo + 3 do envio de teste/monitor), ~45 s com Docker; 3 execuções seguidas iguais; quebra proposital do aviso "aparelho pronto" detectada com a etapa e o esperado/obtido; nenhuma tentativa de rede real no log.
- Comportamentos reais descobertos pela verificação e documentados: responder orçamento já respondido → **409**; Philco/Britania com conserto → orçamento **aprovado automaticamente** (sem WhatsApp).
- `tvs_whatsapp_messages_total` passou para `WhatsAppMetrics` (Infrastructure), compartilhado pelo worker e pela rota de teste — evita dois instrumentos com o mesmo nome.
- Validado com Docker (Evolution do profile `whatsapp`): métrica `tvs_whatsapp_connected 0`, logs "desconectado (instância em "close")" e "indisponível (…)", alerta "WhatsApp desconectado" em *firing* ~5 min após parar a Evolution; envio de teste: 400 sem número, 502 com o motivo da Evolution, 404 pelo Caddy.
