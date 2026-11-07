# Implementation Plan: Busca de clientes em memória

**Branch**: `021-customer-search-cache` | **Date**: 2026-11-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/021-customer-search-cache/spec.md`

## Summary

A barra "Buscar cliente" (Criar ordem e Criar venda) hoje faz `LIKE '%termo%'` na tabela
`Customers` a cada letra digitada, o que percorre os cerca de 30 mil registros. A busca passa a
ser respondida por um **snapshot em memória** (`IMemoryCache`) com ID, nome, telefone principal e
uma chave de busca normalizada (sem acento, maiúsculas). O snapshot é montado sob demanda, uma
vez só, e expira em 1 hora.

**Ao criar ou editar um cliente com sucesso, o cache é invalidado** (pedido explícito do usuário).
Um contador de geração impede que uma montagem que estava em andamento grave dados velhos. A
ficha completa passa a ser buscada por um novo `GET /customers/{id}` quando o operador seleciona
um resultado.

## Technical Context

**Language/Version**: C# / .NET 9 (`net9.0`) no backend; TypeScript / Angular no frontend (`web/`)

**Primary Dependencies**: ASP.NET Core minimal APIs, EF Core 9 (SQL Server),
`Microsoft.Extensions.Caching.Memory` (`IMemoryCache`, já disponível via ASP.NET Core / EF Core,
sem pacote novo)

**Storage**: SQL Server (tabela `Customers`, sem mudança de schema) + memória do processo da API

**Testing**: MSTest em `src/TVSApp.Tests` (unidade, com fakes) e `src/TVSApp.IntegrationTests`
(fluxos HTTP, `ClientesTests`)

**Target Platform**: Linux (container Docker `tvs-app-v2-api`, `mem_limit: 512m`)

**Project Type**: web-service (API) + web app (Angular)

**Performance Goals**: busca em memória em menos de 5 ms no servidor; p95 abaixo de 300 ms na tela
(SC-001); montagem do snapshot abaixo de 3 s após reinício (SC-006)

**Constraints**: até 20 MB a mais de memória com 30 mil clientes (SC-005); a busca seguinte a uma
gravação sempre vê o dado novo (FR-005); uma única instância da API

**Scale/Scope**: cerca de 30 mil clientes hoje, com folga para 60 mil; dezenas de cadastros e
edições por dia; 2 telas afetadas (via `CustomerPickerComponent`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Princípio / Gate | Situação | Como é atendido |
|---|------------------|----------|-----------------|
| I | Clean Architecture, dependências para dentro | ✅ | Interface `ICustomerSearchCache`, entrada e regra de busca na **Application**. Implementação com `IMemoryCache`/EF na **Infrastructure**. DI e endpoint na **Api**, sem regra de negócio. |
| II | Domain isolado | ✅ | Domain sem mudanças (só remove `GetCustomerByName` de `ICustomerQueryRepository`). Nenhuma referência a cache no Domain. |
| III | EF Core + migrations | ✅ | Sem mudança de schema, sem migration. A montagem usa EF Core com projeção `AsNoTracking`. |
| IV | Regras de negócio testadas | ✅ | Testes de unidade para normalização, busca, invalidação no handler e concorrência do cache. Integração para cadastrar → buscar → renomear. |
| V | Interface em português | ✅ | Mensagens novas em pt-BR ("Não foi possível abrir o cliente. Tente de novo."). As existentes não mudam. |
| — | Nova tecnologia | ✅ | Nenhuma. `IMemoryCache` faz parte do framework em uso. |
| — | Quality gates | ✅ | `dotnet build`/`dotnet test` e build do `web/` entram nas tasks. |

**Resultado (pré-research)**: PASS. **Re-check pós-design (Phase 1)**: PASS. O design de
[data-model.md](./data-model.md) e [contracts/](./contracts/customers-api.md) não adiciona
projeto, dependência ou camada.

## Design

### Fluxo de busca

```text
GET /customers?name=jose concei
  └─ CustomerQueryHandler.SearchByName            (Application)
       ├─ termo = CustomerNameNormalizer.Normalize("jose concei") → "JOSE CONCEI"
       ├─ termo.Length < 3 ? → 400 "Digite pelo menos 3 letras."
       ├─ entries = await ICustomerSearchCache.GetEntriesAsync()
       │     └─ CustomerSearchCache (Infrastructure, singleton)
       │          cache hit → snapshot
       │          miss → semáforo → double-check → geração g = atual
       │                 → novo escopo → SELECT Id, Name, Phone ORDER BY Id DESC
       │                 → CustomerSearchEntry.Create(...) para cada linha
       │                 → se geração == g: grava (expira em 1 h); senão: só devolve
       ├─ entries.Where(e => e.SearchKey.Contains(termo, Ordinal)).Take(21)
       └─ → List<CustomerSearchItemDto>
```

### Invalidação (pedido do usuário)

```text
POST /customers         → CustomerCommandHandler.Create
PUT  /customers/{id}    → CustomerCommandHandler.Update
   await _customerCommandRepository.CreateAsync/UpdateAsync(customer);   // exceção → não invalida
   _customerSearchCache.Invalidate();                                     // geração++ e remove a entrada
   return ...
```

- `Invalidate()` é chamado **depois** do commit no banco e **antes** da resposta HTTP. Qualquer
  busca iniciada depois que o operador vê "Cliente cadastrado" monta o snapshot com o dado novo.
- `Update` retorna 404 antes de gravar quando o cliente não existe, e nesse caso não invalida.
- São os **únicos** pontos de gravação de `Customers` no código (verificado: só
  `CustomerCommandRepository` chama `Customers.AddAsync`/`Update`). Qualquer novo ponto de
  gravação de cliente no futuro também precisa chamar `Invalidate()`. Isso vai como comentário
  XML na interface `ICustomerSearchCache`.

Detalhes e alternativas: [research.md](./research.md) R2–R4.

### Componentes

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Application | `Interfaces/Services/ICustomerSearchCache.cs` | **novo**: `Task<IReadOnlyList<CustomerSearchEntry>> GetEntriesAsync(CancellationToken)`, `void Invalidate()` |
| Application | `Search/CustomerSearchEntry.cs` | **novo**: record imutável + `Create(id, name, phone)` |
| Application | `Search/CustomerNameNormalizer.cs` | **novo**: `Normalize(string?)` (trim, espaços, sem acento, maiúsculas) |
| Application | `DTOs/CustomerSearchItemDto.cs` | **novo**: `{ Id, Name, Phone }` |
| Application | `Handlers/Customers/CustomerQueryHandler.cs` | `SearchByName` usa o cache e retorna `List<CustomerSearchItemDto>`. **Novo** `GetById(long)` |
| Application | `Handlers/Customers/ICustomerQueryHandler.cs` | assinaturas atualizadas |
| Application | `Handlers/Customers/CustomerCommandHandler.cs` | recebe `ICustomerSearchCache` e chama `Invalidate()` após `Create`/`Update` bem-sucedidos |
| Domain | `Repositories/Customers/ICustomerQueryRepository.cs` | remove `GetCustomerByName` |
| Infrastructure | `Repositories/Customers/CustomerQueryRepository.cs` | remove `GetCustomerByName` |
| Infrastructure | `Caching/CustomerSearchCache.cs` | **novo**: singleton com `IMemoryCache`, `IServiceScopeFactory`, `SemaphoreSlim`, geração e log de montagem |
| Api | `Common/BuilderExtensions.cs` | `AddMemoryCache()`, `AddSingleton<ICustomerSearchCache, CustomerSearchCache>()` |
| Api | `Endpoints/Customers/CustomerQueryEndpoints.cs` | **novo** `GET /customers/{id:long}` |
| Tests | `Handlers/Fakes/FakeCustomerSearchCache.cs` | **novo**: entradas configuráveis + contador de `Invalidate` |
| Tests | `Handlers/Fakes/FakeCustomerQueryRepository.cs` | remove `GetCustomerByName` |
| Tests | `Handlers/CustomerHandlerTests.cs` | testes de busca e de invalidação |
| Tests | `Application/CustomerNameNormalizerTests.cs` | **novo** |
| Tests | `Infrastructure/CustomerSearchCacheTests.cs` | **novo**: montagem única, geração, falha (loader injetável) |
| IntegrationTests | `Fluxos/ClientesTests.cs` | cadastrar → buscar → renomear → buscar; `GET /customers/{id}` |
| Web | `features/customers/models/customer-search-item.model.ts` | **novo** |
| Web | `features/customers/customers.service.ts` | `searchCustomers` tipado. **Novo** `getCustomer(id)` |
| Web | `features/customers/customer-search/customer-search.component.ts` | `results`/`selected` usam `CustomerSearchItem` |
| Web | `features/customers/customer-picker/customer-picker.component.ts` | `openExisting` busca a ficha por id antes de abrir o diálogo |

Para testar o `CustomerSearchCache` sem banco, a leitura fica atrás de um delegate ou interface
interna (`ICustomerSearchSource` na Infrastructure: `Task<List<CustomerSearchEntry>> LoadAsync`).
A implementação real abre o escopo e consulta o `ApplicationDataContext`. Os testes usam um source
fake, com atraso controlado e opção de lançar exceção.

## Project Structure

### Documentation (this feature)

```text
specs/021-customer-search-cache/
├── spec.md
├── plan.md                 # este arquivo
├── research.md             # Phase 0
├── data-model.md           # Phase 1
├── quickstart.md           # Phase 1
├── contracts/
│   └── customers-api.md    # Phase 1
├── checklists/
│   └── requirements.md
└── tasks.md                # Phase 2 (/speckit-tasks; ainda não criado)
```

### Source Code (repository root)

```text
src/
├── TVSApp.Domain/
│   └── Repositories/Customers/ICustomerQueryRepository.cs        (remove GetCustomerByName)
├── TVSApp.Application/
│   ├── DTOs/CustomerSearchItemDto.cs                              (novo)
│   ├── Search/CustomerSearchEntry.cs                              (novo)
│   ├── Search/CustomerNameNormalizer.cs                           (novo)
│   ├── Interfaces/Services/ICustomerSearchCache.cs                (novo)
│   └── Handlers/Customers/{CustomerQueryHandler,CustomerCommandHandler,ICustomerQueryHandler}.cs
├── TVSApp.Infrastructure/
│   ├── Caching/CustomerSearchCache.cs                             (novo)
│   ├── Caching/CustomerSearchSource.cs                            (novo)
│   └── Repositories/Customers/CustomerQueryRepository.cs          (remove GetCustomerByName)
├── TVSApp.Api/
│   ├── Common/BuilderExtensions.cs
│   └── Endpoints/Customers/CustomerQueryEndpoints.cs
├── TVSApp.Tests/
│   ├── Application/CustomerNameNormalizerTests.cs                 (novo)
│   ├── Infrastructure/CustomerSearchCacheTests.cs                 (novo)
│   └── Handlers/{CustomerHandlerTests.cs, Fakes/FakeCustomerSearchCache.cs (novo), Fakes/FakeCustomerQueryRepository.cs}
└── TVSApp.IntegrationTests/
    └── Fluxos/ClientesTests.cs

web/src/app/features/customers/
├── models/customer-search-item.model.ts                           (novo)
├── customers.service.ts
├── customer-search/customer-search.component.{ts,html}
└── customer-picker/customer-picker.component.ts
```

**Structure Decision**: segue a estrutura existente (Clean Architecture em `src/` e Angular em
`web/`), sem projeto novo. O código novo de cache fica em `TVSApp.Infrastructure/Caching/`, uma
pasta nova ao lado de `Messaging/` e `Services/`.

## Riscos e mitigação

| Risco | Mitigação |
|-------|-----------|
| Montagem em andamento grava snapshot velho após invalidação | Contador de geração (research R3), coberto por teste |
| Várias montagens simultâneas após reinício | `SemaphoreSlim` com double-check (R4), coberto por teste |
| Gravação de cliente por outro caminho no futuro | Comentário na interface e expiração de 1 h como rede de segurança |
| Escalar para mais de uma instância da API | Fora de escopo (Assumptions da spec). Exigiria invalidação distribuída |
| Falha do banco na montagem devolve a mensagem crua da exceção (ex.: `SqlException`) | Comportamento pré-existente do `ExceptionMiddleware` para qualquer erro 500 em produção (`message = ex.Message`), igual à busca antiga. Fora do escopo desta feature; a tela cai no texto da API. Melhoria sugerida: mensagem genérica em pt-BR para 500 em produção |
| Primeira busca após cada cadastro fica mais lenta (remonta) | Aceito: medido no quickstart. Atualização incremental fica como otimização futura (R2) |

## Complexity Tracking

Nenhuma violação da constituição. Seção não aplicável.
