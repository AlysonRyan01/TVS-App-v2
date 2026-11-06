# Implementation Plan: Limpeza das rotas da API

**Branch**: `014-rest-routes-cleanup` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/014-rest-routes-cleanup/spec.md`

## Summary

Deixar a API só com rotas usadas, em REST e separadas em comandos e consultas por recurso. Saem 9
rotas sem uso (e o código que só elas usavam, inclusive todo o MediatR e o envio de e-mail);
renomeiam-se estimativa, autenticação e pagamentos; as rotas do site público mantêm o caminho, mas
a consulta do cliente (hoje quebrada) e o aprovar/reprovar (hoje inexistentes) são reescritos sem
MediatR, conferindo o código de segurança. Sem migration. Decisões em [research.md](research.md);
mapa final em [data-model.md](data-model.md).

## Technical Context

**Language/Version**: C# / .NET 9; TypeScript / Angular 22

**Primary Dependencies**: ASP.NET Core Minimal API, EF Core, ASP.NET Identity + JWT, SignalR; MediatR **removido**

**Storage**: SQL Server, sem mudança de schema

**Testing**: MSTest + fakes (novos handlers); builds; roteiro manual (sistema interno + site)

**Target Platform**: API Linux (Docker) + SPA + site estático `tvseletronica`

**Project Type**: web application (`src/` + `web/`)

**Performance Goals**: n/a

**Constraints**: site público sem alterações; nada muda no banco; REST; pt-BR

**Scale/Scope**: 44 → 37 rotas; 7 arquivos de endpoints → 9 (comando/consulta por recurso); 5 handlers novos (consulta do cliente, aprovar/reprovar pelo cliente, pagamentos); ~25 arquivos removidos

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Como é atendido |
|-----------|--------|-----------------|
| I. Clean Architecture | ✅ | Endpoints só compõem; regras nos handlers da Application; remoção de código morto em todas as camadas. |
| II. Domain isolado | ✅ | Domain não muda (só sai `GetByEnterpriseAsync`/`GetAllAsync` das interfaces). |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras de negócio testadas | ✅ | Testes de `GetForCustomer`, `Approve/RejectEstimateByCustomer` (inclusive código errado) e dos 4 de pagamentos. |
| V. UI em português | ✅ | Mensagens em pt-BR. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/014-rest-routes-cleanup/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/http-endpoints.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/TVSApp.Api/
├── Endpoints/
│   ├── ServiceOrders/ServiceOrderCommandEndpoints.cs, ServiceOrderQueryEndpoints.cs   # NOVO (de ServiceOrderEndpoints.cs)
│   ├── Customers/CustomerCommandEndpoints.cs, CustomerQueryEndpoints.cs             # NOVO (de CustomerEndpoints.cs)
│   ├── Auth/AuthCommandEndpoints.cs                                                 # NOVO (de AuthEndpoints.cs)
│   ├── Metrics/MetricsQueryEndpoints.cs                                             # NOVO (de DashboardEndpoints.cs)
│   ├── Public/PublicCommandEndpoints.cs, PublicQueryEndpoints.cs                    # NOVO (site; inclui WhatsAppEndpoints.cs)
│   └── {ServiceOrder,Customer,Auth,Dashboard,Notification,Email,WhatsApp}Endpoints.cs # REMOVER
├── Common/AppExtensions.cs                                                          # chama os novos Map*
└── Common/BuilderExtensions.cs                                                      # − MediatR, − e-mail

src/TVSApp.Application/
├── Handlers/ServiceOrderCommandHandler.cs                # + ApproveEstimateByCustomer, RejectEstimateByCustomer, Add/Update/RemovePayment
├── Handlers/ServiceOrderQueryHandler.cs                  # + GetForCustomer, GetPayments
├── Requests/ServiceOrderRequests/...                     # commands de pagamento sem MediatR; CustomerEstimateResponseRequest
└── (removidos: handlers/requests de notificação, métricas abandonadas, clientes paginados/por id, e-mail, queries sem uso)

src/TVSApp.Infrastructure/                                # − EmailQueue, EmailNotificationWorker, EmailService; − GetByEnterpriseAsync, GetAllAsync (cliente), GetAbandonedOrdersAsync
src/TVSApp.Tests/Handlers/                                # + testes dos handlers novos

web/src/app/
├── core/auth/*                                           # /auth/login, /auth/logout, /auth/refresh
└── features/home/modals/estimate/*                       # PUT /service-orders/{id}/estimate
```

**Structure Decision**: uma pasta por recurso em `Endpoints/`, com arquivo de comandos e de
consultas (R1). Rotas do site isoladas em `Public/`.

### Ordem de implementação sugerida

1. Testes + handlers novos (site e pagamentos).
2. Novos arquivos de endpoints (comando/consulta) com todas as rotas mantidas/renomeadas; remover
   os antigos.
3. Remover código morto (rotas cortadas, MediatR, e-mail); build.
4. Angular: estimativa e auth.
5. Quickstart (sistema interno + site sem mudanças).

## Complexity Tracking

Nenhuma violação. As 4 rotas do site ficam fora do padrão REST por decisão do usuário (site não muda).
