# Research: Verificação automática do sistema

Estado atual: 202 testes unitários (MSTest, com fakes) em `TVSApp.Tests`; nenhum teste de
integração; sem CI. WhatsApp: `WhatsappService` (typed `HttpClient`) chama a Evolution
(`POST {BaseUrl}/message/sendText/{Instance}` com `{ number, text, ... }`, cabeçalho `apikey`);
fila em memória + `WhatsAppNotificationWorker`. Em `Development` os textos vão para um número fixo
de teste; o aviso ao dono usa número fixo só em `Production`. A API aplica migrations e cria o
usuário inicial ao subir.

## R1. Como subir o sistema real na verificação

- **Decision**: `WebApplicationFactory<Program>` (`Microsoft.AspNetCore.Mvc.Testing`) com
  `Program` exposto via `public partial class Program;`. Configuração sobrescrita: connection
  string do container, JWT de teste, `Admin__Email/Password` de teste, CORS, ambiente
  **`Production`** (mesmo comportamento do servidor, inclusive número do dono).
- **Rationale**: exercita o pipeline real (middlewares, validação, handlers, EF, PDFs, fila,
  worker, eventos) pelas mesmas rotas que o Angular e o site usam.
- **Alternatives**: chamar handlers direto (já coberto pelos unitários; não pega rota/serialização);
  subir via `docker compose` e testar por HTTP (mais lento, difícil capturar o WhatsApp).

## R2. Banco descartável

- **Decision**: `Testcontainers.MsSql` (imagem `mcr.microsoft.com/mssql/server:2022-latest`), um
  container por execução (`[AssemblyInitialize]`), banco novo; a própria API aplica as migrations.
  Cada cenário cria seus próprios cliente/OS → cenários independentes entre si.
- **Rationale**: SQL Server real (migrations e consultas idênticas à produção); nada toca o Azure.
- **Alternatives**: SQLite/InMemory (incompatíveis com migrations SQL Server); banco local fixo
  (estado sujo entre execuções).

## R3. Capturar o WhatsApp sem enviar (defesa em duas camadas)

- **Decision**:
  1. `ConfigureTestServices` reconfigura o typed client `IWhatsappService` com
     `ConfigurePrimaryHttpMessageHandler` → `FakeEvolutionHandler`, que grava cada requisição
     (rota, instância, `number`, `text`, `mediatype/fileName` de mídia) e responde 200.
  2. `EvolutionApiSettings__BaseUrl = http://evolution.invalid` — se o handler falso não for
     aplicado, o DNS `.invalid` falha e nada é enviado.
  Telefones dos clientes de teste: números fictícios (ex.: `41900000001`).
- **Rationale**: confere o texto e o número **reais** montados pelo `WhatsappService` (FR-002),
  inclusive formatação do telefone, e garante SC-005.
- **Espera**: o envio é assíncrono (fila + worker); a verificação aguarda até 10 s por mensagem
  esperada e, para "nenhuma mensagem", espera 2 s e confirma vazio.

## R4. Saída da verificação

- **Decision**: cenários MSTest com nomes em pt-BR (`[TestMethod("Entrega com garantia envia WhatsApp de garantia")]`);
  asserts com mensagens pt-BR "Etapa X: esperado … / obtido …"; rodar com
  `dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"` lista cada
  cenário aprovado/reprovado e o total. Comando único documentado no README.
- **Rationale**: sem ferramenta nova; resumo legível (FR-004).

## R5. Estado da conexão do WhatsApp

- **Decision**: `GET {BaseUrl}/instance/connectionState/{Instance}` (cabeçalho `apikey`) →
  `instance.state` = `open` (conectado) | `connecting` | `close`. Mapeamento: `open` →
  **conectado**; `connecting`/`close` → **desconectado**; erro HTTP/timeout/chave inválida →
  **indisponível**. `WhatsAppConnectionMonitor` (BackgroundService) consulta a cada 60 s
  (configurável), publica `ObservableGauge` `whatsapp_connected` no meter `TVS` (→
  `tvs_whatsapp_connected` 1/0) e registra log só **quando o estado muda**
  (`WhatsApp conexão: {State} ({Reason})`). Sem `BaseUrl` configurado: monitor desligado e sem
  métrica (alerta fica em "OK"/sem dados).
- **Rationale**: não envia mensagens (FR-006); detecção ≤ 1 min + `for: 5m` no alerta ≤ 10 min (SC-004).
- **Alternatives**: webhook `CONNECTION_UPDATE` da Evolution (exige rota pública e configuração na
  Evolution; não detecta Evolution fora do ar).

## R6. Envio real de teste

- **Decision**: rota `POST /internal/whatsapp/test-message` (prefixo `/internal`, já bloqueado pelo
  Caddy para a internet → só acessível dentro do Docker; sem login), executada na VPS com
  `docker compose exec api curl -s -X POST localhost:8080/internal/whatsapp/test-message`. Envia
  **síncrono** (resposta traz enviado/falhou + motivo) texto "🧪 *Mensagem de teste* do sistema TVS
  Eletrônica — {data/hora}. Se você recebeu, o envio de WhatsApp está funcionando." para
  `WhatsAppTest:Phone` (`WHATSAPP_TEST_PHONE`). Sem número → 400 "Configure WHATSAPP_TEST_PHONE…".
  Registra evento `whatsapp.test` e `tvs_whatsapp_messages_total{type="TestMessage"}`.
- **Rationale**: sob demanda (Q2), não cria cliente/OS (FR-009), não exposto à internet, sem UI nova.
- **Alternatives**: botão no Angular (exige permissão/admin e UI; pode vir depois reaproveitando a
  rota); rota pública autenticada (amplia superfície sem necessidade).

## R7. Execução sob demanda

- **Decision** (Clarificação Q1): sem pipeline nem execução na VPS; o README instrui rodar antes de
  publicar. O projeto entra no `TVSApp.sln` (compila junto), mas o gate rápido continua
  `dotnet test src/TVSApp.Tests`.
