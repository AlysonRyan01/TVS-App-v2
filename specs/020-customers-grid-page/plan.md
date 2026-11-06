# Implementation Plan: Tela Clientes no padrão da tela de Ordens

**Branch**: `020-customers-grid-page` | **Date**: 2026-11-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/020-customers-grid-page/spec.md`

## Summary

Nova rota `GET /customers/search` (nome, telefone, CPF/CNPJ, paginação; mais recentes primeiro) seguindo o
mesmo padrão da busca de OS (filtro no Domain → repositório EF → handler com validação → `PaginatedResult`).
No Angular, a tela Clientes é refeita no layout da tela de Ordens (filtros no topo, `mat-table` com
paginação de 20, clique abre o diálogo de edição existente, atualização em tempo real) e o estilo comum
das duas telas vai para um mixin compartilhado. "Buscar OS" vira **"Ordens"** (menu, título, aba, rota
`/ordens` com `/buscar` redirecionando). Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 9 (API) · TypeScript / Angular 22 (web)

**Primary Dependencies**: nenhuma nova (EF Core `EF.Functions.Collate`, Angular Material table/paginator já usados)

**Storage**: tabela `Customers` existente; **sem mudança de schema**

**Testing**: unitários do handler em `TVSApp.Tests/Handlers/CustomerHandlerTests.cs` (validações e normalização); integração em `TVSApp.IntegrationTests` (busca real no SQL Server: nome sem acento, telefone formatado, documento, paginação, ordem); build do Angular + verificação visual no navegador (desktop e celular)

**Target Platform**: navegadores (desktop e celular)

**Project Type**: web application (API + Angular)

**Performance Goals**: primeira página em ≤ 2 s (SC-002); consulta paginada no banco (`Skip/Take` + `Count`)

**Constraints**: rota antiga `GET /customers?name=` continua (usada pelo seletor de cliente ao abrir OS/venda); telefones gravados com ou sem pontuação (dados do sistema antigo); sistema antigo intacto

**Scale/Scope**: 1 rota + filtro + método de repositório + handler; tela Clientes refeita; renomear 1 tela; mixin de estilo compartilhado

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / Gate | Status | Como é atendido |
|------------------|--------|-----------------|
| I. Clean Architecture | ✅ | `CustomerSearchFilter` + contrato no Domain (como `ServiceOrderSearchFilter`); validação no handler (Application); consulta EF na Infrastructure; endpoint só compõe. |
| II. Domain isolado | ✅ | Domain ganha só um record de filtro e uma assinatura de repositório. |
| III. EF Core + migrations | ✅ | Sem mudança de schema. |
| IV. Regras testadas | ✅ | Validações (nome ≥ 3 letras, dígitos, paginação) com testes unitários; consulta real com testes de integração. |
| V. Português | ✅ | Telas, mensagens e títulos em pt-BR. |
| Gates de build | ✅ | `dotnet build`/`dotnet test` + `npm run build`. |

**Post-design re-check**: ✅ sem violações.

## Project Structure

### Documentation (this feature)

```text
specs/020-customers-grid-page/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/customers-search.md
├── checklists/requirements.md
└── tasks.md            # /speckit-tasks
```

### Source Code (repository root)

```text
src/TVSApp.Domain/Repositories/Customers/
├── CustomerSearchFilter.cs                 # NOVO: Name?, PhoneDigits?, DocumentDigits?
└── ICustomerQueryRepository.cs             # + SearchAsync(filter, pageNumber, pageSize)
src/TVSApp.Application/
├── Requests/CustomerRequests/CustomerQueries/CustomerSearchQuery.cs   # NOVO (query string)
└── Handlers/CustomerQueryHandler.cs (+ interface)                     # + Search(query)
src/TVSApp.Infrastructure/Repositories/Customers/CustomerQueryRepository.cs  # + SearchAsync
src/TVSApp.Api/Endpoints/Customers/CustomerQueryEndpoints.cs                 # + GET /customers/search
src/TVSApp.Tests/Handlers/{CustomerHandlerTests.cs, Fakes/FakeCustomerQueryRepository.cs}
src/TVSApp.IntegrationTests/Fluxos/ClientesTests.cs                          # NOVO

web/src/app/
├── shared/styles/_list-page.scss                         # NOVO: mixin do layout filtros + grade + rodapé
├── features/customers/customers-page/*                   # tela refeita
├── features/customers/customers.service.ts               # + searchPaged()
├── features/customers/models/customer-search.model.ts    # NOVO
├── features/search-service-orders/*                      # título "Ordens"; usa o mixin
├── shared/layout/base-layout/base-layout.component.ts    # menu "Ordens"
└── app.routes.ts                                         # /ordens + redirect de /buscar

README.md (rota nova, tela Ordens), DEPLOY.md (nada)
```

**Structure Decision**: espelhar a busca de OS em todas as camadas; extrair o CSS comum das duas telas
para não duplicar.

### Ordem de implementação sugerida

1. Backend: filtro, repositório, handler (TDD com fakes), endpoint, testes de integração.
2. Angular: mixin de layout, serviço, tela Clientes, tempo real.
3. Renomear para "Ordens" (menu, título, rota + redirect).
4. Docs e validação visual (desktop e celular).

## Complexity Tracking

Nenhuma violação.
