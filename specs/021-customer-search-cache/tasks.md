---
description: "Task list for 021-customer-search-cache"
---

# Tasks: Busca de clientes em memória

**Input**: Design documents from `/specs/021-customer-search-cache/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/customers-api.md](./contracts/customers-api.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUÍDOS. A constituição (Princípio IV, NÃO NEGOCIÁVEL) exige testes automatizados em
`src/TVSApp.Tests` para toda regra de negócio nova ou alterada, e o plan.md lista os testes.

**Organization**: tarefas agrupadas por user story da spec (US1 busca rápida, US2 invalidação
ao criar/editar, US3 reinício e montagem única).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: user story da tarefa (US1, US2, US3)

## Path Conventions

- Backend: `src/TVSApp.{Domain,Application,Infrastructure,Api}/`, testes em `src/TVSApp.Tests/` e `src/TVSApp.IntegrationTests/`
- Frontend: `web/src/app/features/customers/`
- Namespaces seguem o padrão existente: `TVS_App.Application.*`, `TVS_App.Infrastructure.*`, `TVS_App.Api.*`, testes em `TVSApp.Tests.*`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: confirmar o ponto de partida. Não há pacote, projeto ou migration novos (plan.md → Technical Context).

- [X] T001 Rodar `dotnet build` e `dotnet test src/TVSApp.Tests` na raiz e anotar o total de testes que passam hoje (linha de base, esperado 246). Confirmar com `grep -rn "Customers\.\(AddAsync\|Add\|Update\|Remove\)" src --include=*.cs` que os únicos pontos de gravação de clientes são `CreateAsync`/`UpdateAsync` em `src/TVSApp.Infrastructure/Repositories/Customers/CustomerCommandRepository.cs` (research.md, "Situação atual"). Se houver outro ponto, registrar no plan.md, porque ele também precisará chamar `Invalidate()`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: tipos da Application, implementação do cache na Infrastructure e registro no DI. Todas as user stories dependem disso.

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase terminar.

- [X] T002 [P] Criar `CustomerNameNormalizer` (static) em `src/TVSApp.Application/Search/CustomerNameNormalizer.cs`, namespace `TVS_App.Application.Search`, com `public static string Normalize(string? value)`: `null`/vazio → `""`; `Trim`; troca qualquer sequência de espaços em branco por um espaço só; `Normalize(NormalizationForm.FormD)` e remoção dos caracteres com `CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark` (assim `Ç`→`C`, `É`→`E`); recompor com `NormalizationForm.FormC`; `ToUpperInvariant()`. Não tratar `%` nem `_`: eles ficam literais (research.md R5, FR-011).
- [X] T003 [P] Criar testes em `src/TVSApp.Tests/Application/CustomerNameNormalizerTests.cs` (MSTest, `[TestClass]`), cobrindo: `"José Conceição"` → `"JOSE CONCEICAO"`; `"  joão   da  silva "` → `"JOAO DA SILVA"`; `null` e `"   "` → `""`; `"ÂÃÀÁÄ êëè ñ"` → `"AAAAA EEE N"`; `"50%_off"` → `"50%_OFF"`; texto já normalizado não muda.
- [X] T004 [P] Criar `CustomerSearchEntry` em `src/TVSApp.Application/Search/CustomerSearchEntry.cs`: `public sealed record CustomerSearchEntry(long Id, string Name, string Phone, string SearchKey)` com `public static CustomerSearchEntry Create(long id, string? name, string? phone)`, onde `name ?? ""`, `phone ?? ""` e `SearchKey = CustomerNameNormalizer.Normalize(name)` (data-model.md → CustomerSearchEntry). O tipo é imutável.
- [X] T005 [P] Criar `CustomerSearchItemDto` em `src/TVSApp.Application/DTOs/CustomerSearchItemDto.cs`: `public record CustomerSearchItemDto(long Id, string Name, string Phone);` (contracts/customers-api.md, item de `GET /customers?name=`).
- [X] T006 Criar a interface `ICustomerSearchCache` em `src/TVSApp.Application/Interfaces/Services/ICustomerSearchCache.cs` com `Task<IReadOnlyList<CustomerSearchEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);` (retorna todas as entradas ordenadas por `Id` decrescente) e `void Invalidate();`. Incluir um comentário XML na interface: "Todo código que grava (cria/edita/exclui) clientes MUST chamar Invalidate() após gravar com sucesso." Depende de T004.
- [X] T007 Criar, na Infrastructure, a interface pública `ICustomerSearchSource` (`Task<List<CustomerSearchEntry>> LoadAsync(CancellationToken ct)`) e a implementação `CustomerSearchSource` em `src/TVSApp.Infrastructure/Caching/CustomerSearchSource.cs`. Ela recebe `IServiceScopeFactory`, abre um escopo, resolve `ApplicationDataContext` e executa `context.Customers.AsNoTracking().OrderByDescending(c => c.Id).Select(c => new { c.Id, Name = c.Name.CustomerName, Phone = c.Phone.CustomerPhone }).ToListAsync(ct)`, mapeando cada linha com `CustomerSearchEntry.Create`. Só projeção, sem materializar `Customer` (research.md R6). Depende de T004.
- [X] T008 Criar `CustomerSearchCache : ICustomerSearchCache` (sealed, pensado para singleton) em `src/TVSApp.Infrastructure/Caching/CustomerSearchCache.cs`, recebendo `IMemoryCache`, `ICustomerSearchSource`, `ILogger<CustomerSearchCache>` e `TimeProvider` (para os testes controlarem o tempo). Comportamento (research.md R1, R3, R4):
  - chave `"customers:search-index"` e `const` `TimeSpan Lifetime = TimeSpan.FromHours(1)`;
  - campo `long _generation` e `SemaphoreSlim _loadLock = new(1, 1)`;
  - `GetEntriesAsync`: (1) `TryGetValue` → retorna; (2) `await _loadLock.WaitAsync(ct)`; (3) `TryGetValue` de novo (double-check) → retorna; (4) `var generation = Interlocked.Read(ref _generation)`; (5) `var stopwatch` + `var entries = (await _source.LoadAsync(ct)).AsReadOnly()`; (6) **só se** `Interlocked.Read(ref _generation) == generation`, `_cache.Set(key, entries, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Lifetime })`; (7) `LogInformation("Índice de busca de clientes montado: {Count} clientes em {ElapsedMs} ms", ...)`; (8) `finally` → `_loadLock.Release()`. Exceções do source sobem sem gravar nada (FR-008);
  - `Invalidate()`: `Interlocked.Increment(ref _generation)`, `_cache.Remove(key)`, `LogDebug("Índice de busca de clientes invalidado")`.
  Depende de T006 e T007.
- [X] T009 Registrar no DI em `src/TVSApp.Api/Common/BuilderExtensions.cs`, perto das linhas 69–74 (registros de cliente): `builder.Services.AddMemoryCache();`, `builder.Services.AddSingleton<ICustomerSearchSource, CustomerSearchSource>();` e `builder.Services.AddSingleton<ICustomerSearchCache, CustomerSearchCache>();`. Se não houver `TimeProvider` registrado (`grep -rn TimeProvider src/TVSApp.Api`), adicionar `builder.Services.TryAddSingleton(TimeProvider.System);`. Depende de T008.
- [X] T010 [P] Criar o fake `FakeCustomerSearchCache : ICustomerSearchCache` em `src/TVSApp.Tests/Handlers/Fakes/FakeCustomerSearchCache.cs`, com `List<CustomerSearchEntry> Entries` (o fake retorna as entradas ordenadas por `Id` decrescente), `int InvalidateCalls` e `int GetEntriesCalls`, mais um helper `Add(long id, string name, string phone = "")` que usa `CustomerSearchEntry.Create`. Depende de T006.

**Checkpoint**: `dotnet build` passa e `CustomerNameNormalizerTests` passa. A busca ainda não usa o cache.

---

## Phase 3: User Story 1 - Encontrar o cliente instantaneamente ao digitar (Priority: P1) 🎯 MVP

**Goal**: `GET /customers?name=` é respondido pela lista em memória (sem acento, ordem `Id` desc, limite 21) e devolve `{ id, name, phone }`. Ao clicar num resultado, a ficha completa vem de `GET /customers/{id}`.

**Independent Test**: com cerca de 30 mil clientes, digitar 3 ou mais letras em Criar ordem e ver a lista quase instantânea, com "jose conceicao" encontrando "JOSÉ CONCEIÇÃO". Clicar num resultado abre a ficha completa (quickstart.md §2, §4, §5).

### Tests for User Story 1 ⚠️

> Escrever primeiro e confirmar que falham (não compilam ou não passam) antes da implementação.

- [X] T011 [US1] Reescrever os testes de busca em `src/TVSApp.Tests/Handlers/CustomerHandlerTests.cs`. Em `Setup`, criar `_searchCache = new FakeCustomerSearchCache()` e passar para o `CustomerQueryHandler`. Substituir `SearchByName_LessThanThreeLetters_Returns400` (termo `"jo"` → 400 "Digite pelo menos 3 letras." e `_searchCache.GetEntriesCalls == 0`; incluir também `"  jo   "`) e `SearchByName_ValidTerm_AsksFor21AndMapsCustomers`. Adicionar: (a) acento: entrada `"JOSÉ CONCEIÇÃO"`, termo `"jose conceicao"` → 1 resultado com `Name == "JOSÉ CONCEIÇÃO"` (nome original preservado); (b) ordem e limite: 30 entradas `"CLIENTE SILVA {i}"` com ids 1..30 → 21 itens, o primeiro com `Id == 30` e o último com `Id == 10`; (c) "contém" no meio: `"MARIA APARECIDA"` encontrado por `"recid"`; (d) sem resultado → 200 e lista vazia; (e) `%`/`_` literais: `"%%%"` não encontra `"JOAO"`; (f) `Phone` copiado para o DTO (vazio continua `""`).
- [X] T012 [P] [US1] Adicionar testes de `GetById` em `src/TVSApp.Tests/Handlers/CustomerHandlerTests.cs`: cliente existente em `_queryRepository.Customers` → 200 com `CustomerDto` completo (nome, endereço, telefones); id inexistente → 404 com a mensagem do repositório.

### Implementation for User Story 1

- [X] T013 [US1] Em `src/TVSApp.Application/Handlers/Customers/ICustomerQueryHandler.cs`: trocar a assinatura para `Task<BaseResponse<List<CustomerSearchItemDto>>> SearchByName(string name);` e adicionar `Task<BaseResponse<CustomerDto?>> GetById(long id);`.
- [X] T014 [US1] Em `src/TVSApp.Application/Handlers/Customers/CustomerQueryHandler.cs`: injetar `ICustomerSearchCache` no construtor (além do `ICustomerQueryRepository` que já existe). Reimplementar `SearchByName`: `var term = CustomerNameNormalizer.Normalize(name)`; se `term.Length < 3`, retornar `new BaseResponse<List<CustomerSearchItemDto>>(null, 400, "Digite pelo menos 3 letras.")` sem chamar o cache. Depois, `var entries = await _searchCache.GetEntriesAsync()`, percorrer em ordem com `SearchKey.Contains(term, StringComparison.Ordinal)`, `Take(SearchLimit)` (mantém `SearchLimit = 21`), mapear para `CustomerSearchItemDto(e.Id, e.Name, e.Phone)` e retornar 200 com a mensagem `"Clientes obtidos com sucesso!"`. Implementar `GetById(long id)`: chama `_customerQueryRepository.GetByIdAsync(id)`; se falhar ou vier `null`, retorna `BaseResponse<CustomerDto?>(null, response.Code, response.Message)`; senão, retorna 200 com `CustomerMapper.MapToDto(customer)` e a mensagem do repositório. Depende de T011–T013.
- [X] T015 [US1] Remover `GetCustomerByName` de `src/TVSApp.Domain/Repositories/Customers/ICustomerQueryRepository.cs`, de `src/TVSApp.Infrastructure/Repositories/Customers/CustomerQueryRepository.cs` (linhas 32–47) e de `src/TVSApp.Tests/Handlers/Fakes/FakeCustomerQueryRepository.cs` (incluindo a propriedade `LastLimit`, se não for mais usada). Remover também `old-handlers/` apenas se ele referenciar o método e fizer parte de algum `.csproj` (conferir com `dotnet build`; se não compila junto, não mexer). Depende de T014.
- [X] T016 [US1] Em `src/TVSApp.Api/Endpoints/Customers/CustomerQueryEndpoints.cs`, adicionar `app.MapGet("/customers/{id:long}", async (ICustomerQueryHandler handler, long id) => (await handler.GetById(id)).ToResult()).WithTags("Customer").RequireAuthorization();`, no mesmo estilo dos endpoints vizinhos. O `GET /customers?name=` continua igual: só o tipo retornado muda. Depende de T014.
- [X] T017 [P] [US1] Criar `web/src/app/features/customers/models/customer-search-item.model.ts` com `export interface CustomerSearchItem { id: number; name: string; phone: string; }`.
- [X] T018 [US1] Em `web/src/app/features/customers/customers.service.ts`: mudar `searchCustomers(name)` para `Observable<BaseResponse<CustomerSearchItem[]>>` e adicionar `getCustomer(id: number): Observable<BaseResponse<Customer>>` chamando `GET ${environment.apiUrl}/customers/${id}`. Depende de T017.
- [X] T019 [US1] Em `web/src/app/features/customers/customer-search/customer-search.component.ts`: `selected = output<CustomerSearchItem>()`, `results = signal<CustomerSearchItem[]>([])` e trocar o import de `Customer` por `CustomerSearchItem`. O template `customer-search.component.html` continua usando `customer.name`/`customer.phone` e não precisa mudar. Confirmar com o build. Depende de T018.
- [X] T020 [US1] Em `web/src/app/features/customers/customer-picker/customer-picker.component.ts`: `openExisting(item: CustomerSearchItem)` passa a chamar `this._service.getCustomer(item.id)`. No sucesso com `result.data`, abre o `CustomerDialogComponent` com `{ ...DIALOG_OPTIONS, data: { customer: result.data } }` e mantém o `afterClosed` atual. No erro, chama `this._snackbar.error('Não foi possível abrir o cliente. Tente de novo.')`. Adicionar um signal `isOpeningCustomer` para ignorar cliques repetidos enquanto a ficha carrega. Ajustar o binding `(selected)` em `customer-picker.component.html`, se o tipo exigir. Depende de T018, T019.
- [X] T021 [US1] Rodar `dotnet build`, `dotnet test src/TVSApp.Tests` (todos passando, incluindo T003/T011/T012) e o build do frontend (`cd web && npx ng build`). Corrigir qualquer outro consumidor de `SearchByName` ou `GetCustomerByName` apontado pelo compilador.

**Checkpoint**: US1 funciona. A busca vem da memória e a ficha abre por id. Cadastros novos ainda não aparecem na busca até o cache expirar; isso é resolvido na US2.

---

## Phase 4: User Story 2 - Cliente recém-cadastrado ou editado aparece na busca na hora (Priority: P1)

**Goal**: **invalidar o cache ao criar/editar cliente** (pedido do usuário). A busca seguinte sempre vê o dado gravado, inclusive quando uma montagem estava em andamento (FR-005).

**Independent Test**: cadastrar "MARIA TESTE ÚNICA", buscar "teste unica" (aparece no topo), renomear e buscar o nome novo (aparece) e o antigo (some) (quickstart.md §3).

### Tests for User Story 2 ⚠️

- [X] T022 [US2] Em `src/TVSApp.Tests/Handlers/CustomerHandlerTests.cs`, passar o `_searchCache` também para o `CustomerCommandHandler` no `Setup` e adicionar: (a) `Create` válido → `_searchCache.InvalidateCalls == 1`; (b) `Update` de cliente existente → `InvalidateCalls == 1`; (c) `Update` de id inexistente (404) → `InvalidateCalls == 0`; (d) `Create` inválido (nome vazio, que lança `CommandException`) → `InvalidateCalls == 0`; (e) repositório lança exceção no `CreateAsync` → exceção sobe e `InvalidateCalls == 0`. Para (e), adicionar ao `src/TVSApp.Tests/Handlers/Fakes/FakeCustomerCommandRepository.cs` uma propriedade `Exception? ThrowOnWrite` usada por `CreateAsync`/`UpdateAsync`.
- [X] T023 [P] [US2] Criar `src/TVSApp.Tests/Infrastructure/CustomerSearchCacheTests.cs` com um `FakeCustomerSearchSource : ICustomerSearchSource` no próprio arquivo, com lista configurável, contador `LoadCalls` e um `TaskCompletionSource? Gate` opcional que segura o `LoadAsync` até ser liberado. Usar `new MemoryCache(new MemoryCacheOptions())`, `NullLogger<CustomerSearchCache>.Instance` e `TimeProvider.System`. Testes: (a) segunda chamada a `GetEntriesAsync` não chama o source de novo (`LoadCalls == 1`); (b) `Invalidate()` seguido de `GetEntriesAsync` → recarrega (`LoadCalls == 2`) e devolve a lista nova; (c) **corrida da geração (research.md R3)**: iniciar `GetEntriesAsync` com `Gate` segurando, chamar `Invalidate()`, trocar a lista do source para incluir um cliente novo, liberar o `Gate` → a primeira chamada devolve a lista velha, mas a chamada seguinte recarrega e contém o cliente novo (`LoadCalls == 2`).

### Implementation for User Story 2

- [X] T024 [US2] Em `src/TVSApp.Application/Handlers/Customers/CustomerCommandHandler.cs`: injetar `ICustomerSearchCache` no construtor. Em `Create`, logo após `await _customerCommandRepository.CreateAsync(customer);`, chamar `_customerSearchCache.Invalidate();`. Em `Update`, logo após `await _customerCommandRepository.UpdateAsync(customer);`, chamar `_customerSearchCache.Invalidate();`. Não invalidar no caminho do 404 nem antes da gravação. Adicionar um comentário curto: `// A busca de clientes (barra "Buscar cliente") vem de um índice em memória; invalida para a próxima busca ver a gravação.` Depende de T022.
- [X] T025 [US2] Conferir que o `CustomerSearchCache` (T008) passa em T023 (c). Se falhar, corrigir a comparação de geração em `src/TVSApp.Infrastructure/Caching/CustomerSearchCache.cs`. Depende de T023.
- [X] T026 [US2] Em `src/TVSApp.IntegrationTests/Fluxos/ClientesTests.cs`, adicionar um teste de fluxo usando os helpers `CreateAsync`/`Token()` que já existem: buscar algo para aquecer o cache (`GET /customers?name=XXX`); cadastrar `"BUSCA {token} ÚNICA"`; `GET /customers?name=busca {token} unica` (minúsculas, sem acento) → contém o id e é o primeiro item; `PUT /customers/{id}` renomeando para `"RENOMEADO {token}"` (mesmo corpo do cadastro, com nome novo); busca pelo nome novo → contém o id; busca pelo antigo → não contém. Adicionar também: `GET /customers/{id}` → 200 com `name` e `street`; `GET /customers/999999999` → 404. Depende de T016, T024.
- [X] T027 [US2] Rodar `dotnet test src/TVSApp.Tests` e `dotnet test src/TVSApp.IntegrationTests --filter "FullyQualifiedName~Clientes"`, com todos passando.

**Checkpoint**: US1 + US2 entregam a feature completa para o dia a dia. Busca rápida e sempre atualizada.

---

## Phase 5: User Story 3 - A busca continua funcionando após reinício da API (Priority: P2)

**Goal**: após um reinício, a lista é montada uma única vez mesmo com buscas simultâneas. Falha na montagem não deixa uma lista vazia em cache (FR-007, FR-008), e o cache expira em 1 hora (FR-009).

**Independent Test**: `docker compose restart api` e buscar na hora. O resultado está correto e o log mostra **uma** linha "Índice de busca de clientes montado" (quickstart.md §2).

### Tests for User Story 3 ⚠️

- [X] T028 [US3] Em `src/TVSApp.Tests/Infrastructure/CustomerSearchCacheTests.cs`, adicionar: (a) **montagem única**: com `Gate` segurando, disparar 10 `GetEntriesAsync` em paralelo (`Task.WhenAll`), liberar → todas recebem a mesma lista e `LoadCalls == 1`; (b) **falha**: source lança `InvalidOperationException` na 1ª chamada → `GetEntriesAsync` lança a mesma exceção; na 2ª chamada o source devolve a lista → retorna a lista (`LoadCalls == 2`), ou seja, nada vazio ficou em cache; (c) **expiração de 1 h**: usar `FakeTimeProvider` (pacote `Microsoft.Extensions.TimeProvider.Testing`, só no projeto de testes) **ou**, se preferir não adicionar pacote, um `MemoryCache` com `Clock` e `ISystemClock` falsos. Avançar 61 min → `GetEntriesAsync` recarrega (`LoadCalls == 2`). Se a opção escolhida exigir que `CustomerSearchCache` use o `TimeProvider` para calcular `AbsoluteExpiration`, ajustar T008 para `AbsoluteExpiration = _timeProvider.GetUtcNow() + Lifetime`. Depende de T023.

### Implementation for User Story 3

- [X] T029 [US3] Ajustar `src/TVSApp.Infrastructure/Caching/CustomerSearchCache.cs` até T028 passar. Em especial: o `catch`/`finally` não pode gravar nada em caso de exceção, e o semáforo deve ser liberado sempre. Confirmar que o `ExceptionMiddleware` (`src/TVSApp.Api/Middlewares/ExceptionMiddleware.cs`) responde 500 com mensagem em português para exceções genéricas. Se não responder, registrar no plan.md, mas não mudar o middleware nesta feature. Depende de T028.

**Checkpoint**: todas as user stories estão funcionando e testadas.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T030 [P] Verificar a constituição: nenhuma referência nova de `TVSApp.Domain` para fora (`grep -rn "using TVS_App\.\(Application\|Infrastructure\|Api\)" src/TVSApp.Domain` vazio) e nenhuma referência a `Microsoft.Extensions.Caching` em `src/TVSApp.Application` (`grep -rn "Caching" src/TVSApp.Application` vazio).
- [X] T031 [P] Atualizar a documentação de clientes, se existir. Procurar com `grep -rln "/customers" README* docs 2>/dev/null` e registrar o novo `GET /customers/{id}` e o formato `{ id, name, phone }` de `GET /customers?name=`.
- [ ] T032 Rodar a validação completa do [quickstart.md](./quickstart.md) num ambiente com volume real (cópia da base de produção): §2 tempos (anotar a duração da montagem que aparece no log, comparar com a estimativa de 100–300 ms do research.md R6 e o SC-006 de 3 s); §3 invalidação; §4 ficha; §5 telas Criar ordem e Criar venda; tela "Clientes" sem regressão (FR-010); §6 memória com `docker stats` (SC-005 ≤ 20 MB).
- [X] T033 Quality gates finais: `dotnet build`, `dotnet test src/TVSApp.Tests`, `dotnet test src/TVSApp.IntegrationTests` e `cd web && npx ng build` sem erros.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup e bloqueia todas as user stories.
- **US1 (Phase 3)**: depende da Phase 2.
- **US2 (Phase 4)**: depende da Phase 2. Pode ser feita em paralelo com a US1 no backend, mas o teste de integração T026 depende de T016 (endpoint `GET /customers/{id}`) e do `SearchByName` novo (T014).
- **US3 (Phase 5)**: depende da Phase 2 e de T023 (arquivo de testes do cache criado na US2).
- **Polish (Phase 6)**: depende de todas as stories.

### Dentro das fases

- T002 → T004 → (T006, T007) → T008 → T009. T003, T005 e T010 em paralelo onde marcado.
- US1: T011/T012 (testes) → T013 → T014 → (T015, T016) e frontend T017 → T018 → T019 → T020 → T021.
- US2: T022/T023 (testes) → T024/T025 → T026 → T027.
- US3: T028 → T029.

### Ordem sugerida das stories

US1 → US2 → US3. **Não fazer deploy de US1 sem US2**: com só a US1, cadastros novos ficariam
até 1 hora sem aparecer na busca e o operador poderia cadastrar duplicados (spec, US2 "Why this
priority").

---

## Parallel Example: Foundational

```text
# Em paralelo (arquivos distintos, sem dependências entre si):
T002 CustomerNameNormalizer.cs
T005 CustomerSearchItemDto.cs
# depois de T002:
T003 CustomerNameNormalizerTests.cs   ‖   T004 CustomerSearchEntry.cs
```

## Parallel Example: User Story 1

```text
# Backend e frontend em paralelo:
Backend:  T011 → T013 → T014 → T015, T016
Frontend: T017 → T018 → T019 → T020
# T012 [P] pode ser escrito junto com T011 (mesmo arquivo: fazer em sequência se for a mesma pessoa)
```

## Parallel Example: User Story 2

```text
T022 CustomerHandlerTests.cs (invalidação no handler)   ‖   T023 CustomerSearchCacheTests.cs (geração)
```

---

## Implementation Strategy

### MVP

1. Phase 1 + Phase 2.
2. Phase 3 (US1) e **Phase 4 (US2) juntas**. O mínimo que pode ir para produção é US1 + US2,
   porque uma busca rápida mas desatualizada causa cadastro duplicado.
3. **Parar e validar**: quickstart.md §2–§5.
4. Deploy (`docker compose up -d --build api web`).

### Incremental

- US3 endurece reinício e falhas. O código principal já existe desde a T008, e a US3 acrescenta
  os testes e os ajustes que eles revelarem. Pode ir num segundo deploy.
- Atualização incremental do snapshot, em vez de invalidar tudo, fica fora de escopo (research.md R2).

---

## Notes

- [P] = arquivos diferentes, sem dependência de tarefa incompleta.
- Testes primeiro em cada story; confirmar que falham antes de implementar.
- Mensagens ao usuário em pt-BR (Princípio V).
- Sem migration: nada muda no schema.
- Commit sugerido ao fim de cada fase.
