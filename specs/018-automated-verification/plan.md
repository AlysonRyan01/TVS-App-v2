# Implementation Plan: Verificação automática do sistema (fluxos e WhatsApp)

**Branch**: `018-automated-verification` | **Date**: 2026-11-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/018-automated-verification/spec.md`

## Summary

Três peças:

1. **Verificação de fluxo (US1)** — novo projeto `src/TVSApp.IntegrationTests` que sobe a API real
   em memória (`WebApplicationFactory<Program>`) contra um SQL Server descartável (Testcontainers),
   percorre o ciclo da OS/venda pelas rotas HTTP e confere cada mensagem de WhatsApp capturando as
   chamadas à Evolution API num **handler HTTP falso** (o texto e o número reais montados pelo
   `WhatsappService` são verificados; nada sai da máquina). Um comando: `dotnet test src/TVSApp.IntegrationTests`.
2. **Monitor de conexão (US2)** — `BackgroundService` na Infrastructure consulta o estado da
   instância na Evolution a cada 60 s, publica a métrica `tvs_whatsapp_connected` (0/1), registra
   mudanças de estado nos logs; Grafana ganha painel e alerta "WhatsApp desconectado".
3. **Envio real de teste (US3)** — rota interna `POST /internal/whatsapp/test-message` (só na rede
   do Docker) que envia uma mensagem marcada como teste para `WHATSAPP_TEST_PHONE`, sob demanda,
   via `docker compose exec api curl ...`.

Decisões em [research.md](research.md); contratos em [contracts/](contracts/).

## Technical Context

**Language/Version**: C# / .NET 9

**Primary Dependencies**: novas só no projeto de testes de integração — `Microsoft.AspNetCore.Mvc.Testing` 9.x, `Testcontainers.MsSql`; MSTest (já usado). Produção: nenhuma dependência nova (BCL `System.Diagnostics.Metrics`, `HttpClient`)

**Storage**: verificação usa SQL Server 2022 em container descartável; produção inalterada (Azure SQL)

**Testing**: MSTest — unitários em `TVSApp.Tests` (monitor de conexão, mensagem de teste); integração ponta a ponta em `TVSApp.IntegrationTests`

**Target Platform**: máquina do desenvolvedor com Docker (verificação); VPS Linux/Docker (monitor e envio de teste)

**Project Type**: web application (API .NET + Angular) + infraestrutura

**Performance Goals**: verificação completa ≤ 5 min (SC-001); queda do WhatsApp visível ≤ 10 min (SC-004)

**Constraints**: zero envio real e zero escrita em produção pela verificação (SC-005, defesa em duas camadas); monitor não envia mensagens; envio de teste só sob demanda e só para o número configurado; textos em pt-BR

**Scale/Scope**: ~12 cenários de fluxo; 1 serviço de monitor; 1 rota interna; 1 método novo no `IWhatsappService`; 1 painel + 1 regra de alerta; docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / Gate | Status | Como é atendido |
|------------------|--------|-----------------|
| I. Clean Architecture | ✅ (com justificativa) | Monitor e envio de teste na Infrastructure atrás de interfaces da Application; rota na Api só compõe. Novo projeto `TVSApp.IntegrationTests` é de **testes** (fora das 4 camadas) — justificado em Complexity Tracking. |
| II. Domain isolado | ✅ | Domain não muda. |
| III. EF Core + migrations | ✅ | Sem mudança de schema; a verificação aplica as migrations existentes num banco descartável (valida que sobem do zero). |
| IV. Regras testadas | ✅ | Lógica nova (interpretação do estado da conexão, mensagem de teste, recusa sem número) com testes unitários em `TVSApp.Tests`. |
| V. Português | ✅ | Mensagem de teste, logs, nomes dos cenários, painel e alerta em pt-BR. |
| Nova tecnologia | ✅ (justificada) | `Mvc.Testing` e `Testcontainers` só no projeto de testes (research R1/R2). |
| Gates de build | ✅ | `dotnet build TVSApp.sln` inclui o novo projeto; `dotnet test src/TVSApp.Tests` continua rápido e sem Docker. |

**Post-design re-check**: ✅ sem violações não justificadas.

## Project Structure

### Documentation (this feature)

```text
specs/018-automated-verification/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── verification-command.md     # comando, saída e cenários da verificação
│   ├── internal-test-message.md    # POST /internal/whatsapp/test-message
│   └── whatsapp-connection.md      # métrica, logs, painel e alerta do monitor
├── checklists/requirements.md
└── tasks.md                        # /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── TVSApp.Api/
│   ├── Program.cs                                   # + public partial class Program (para WebApplicationFactory)
│   ├── Endpoints/Internal/InternalWhatsAppEndpoints.cs   # NOVO: POST /internal/whatsapp/test-message
│   └── Common/BuilderExtensions.cs                  # registra monitor + opções
├── TVSApp.Application/
│   ├── Interfaces/Services/IWhatsappService.cs      # + SendTestMessageAsync / GetConnectionStateAsync
│   └── Observability/BusinessEvents.cs              # + whatsapp.test
├── TVSApp.Infrastructure/
│   ├── Services/Whatsapp/WhatsappService.cs         # implementa os 2 métodos novos
│   ├── Services/Whatsapp/WhatsAppConnectionMonitor.cs    # NOVO: BackgroundService + gauge
│   └── Configurations/WhatsAppTestSettings.cs       # NOVO: número de teste, intervalo
├── TVSApp.Tests/Application|Infrastructure/...      # NOVOS testes unitários
└── TVSApp.IntegrationTests/                         # NOVO projeto
    ├── TVSApp.IntegrationTests.csproj
    ├── Infrastructure/{SqlServerContainer, ApiFactory, FakeEvolutionHandler, ApiClient}.cs
    └── Fluxos/{OrdemDeServicoTests, OrcamentoTests, EntregaEPagamentoTests, VendaTests, PublicoTests, ConsultasTests}.cs

observability/grafana/
├── dashboards/whatsapp.json                         # + painel "Conexão"
└── provisioning/alerting/rules.yaml                 # + "WhatsApp desconectado"

docker-compose.yml   # + WHATSAPP_TEST_PHONE → WhatsAppTest__Phone
.env.example, README.md, DEPLOY.md
```

**Structure Decision**: verificação ponta a ponta num projeto de testes separado (exige Docker e é
mais lenta), deixando `TVSApp.Tests` como o gate rápido de sempre. Monitor e envio de teste ficam
no `WhatsappService`/Infrastructure, que já conhece a Evolution.

### Ordem de implementação sugerida

1. `public partial class Program`; projeto `TVSApp.IntegrationTests` com container SQL, factory e handler falso; primeiro cenário (abrir OS → boas-vindas).
2. Demais cenários de fluxo (orçamento, aprovações, Philco/Britania, peça, conserto, entrega, pagamentos, venda, públicas, busca/métricas).
3. `GetConnectionStateAsync` + monitor + métrica + testes unitários; painel e alerta.
4. `SendTestMessageAsync` + rota interna + testes unitários; compose/env.
5. Docs (README, DEPLOY, .env.example) e quickstart.

## Complexity Tracking

| Violação / decisão | Por que é necessária | Alternativa mais simples rejeitada porque |
|--------------------|----------------------|-------------------------------------------|
| Novo projeto `TVSApp.IntegrationTests` (fora das 4 camadas) | Precisa de Docker e leva ~1–2 min; misturar com `TVSApp.Tests` deixaria o gate unitário lento e dependente de Docker | Pastas/categorias dentro de `TVSApp.Tests` obrigariam filtros em todo `dotnet test` e confundiriam o gate da constituição |
| Dependências novas (`Mvc.Testing`, `Testcontainers.MsSql`) | Subir a API real e um SQL Server real descartável | SQLite/InMemory não rodam as migrations SQL Server nem reproduzem o comportamento real |
