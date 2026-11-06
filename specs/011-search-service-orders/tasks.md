# Tasks: Tela "Buscar OS"

**Input**: Design documents from `/specs/011-search-service-orders/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos — a constituição (IV) e a FR-009 exigem testes das regras da busca.

**Organization**: por user story (US1 achar pelos filtros, US2 abrir a OS, US3 sem resultado e erros).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- Namespaces seguem o padrão existente (`TVS_App.*`; testes em `TVSApp.Tests.*`)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (92 verdes) e `cd web && npx ng build` (sem erro)

---

## Phase 2: Foundational (contratos do Domain)

- [X] T002 [P] Criar `src/TVSApp.Domain/Repositories/ServiceOrders/ServiceOrderSearchFilter.cs`: `public record ServiceOrderSearchFilter(long? Number, string? Customer, string? Model, string? SerialNumber, DateTime? From, DateTime? To);` (já normalizado pelo handler: textos com trim, `From`/`To` como datas sem hora)
- [X] T003 [P] Adicionar em `src/TVSApp.Domain/Repositories/Responses.cs`: `public record ServiceOrderSearchItem(long Number, string Customer, EProduct ProductType, string Brand, string Model, string SerialNumber, EEnterprise Enterprise, EServiceOrderStatus ServiceOrderStatus, bool IsSale, DateTime EnteredAt);`
- [X] T004 Em `src/TVSApp.Domain/Repositories/ServiceOrders/IServiceOrderQueryRepository.cs`: adicionar `Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> SearchAsync(ServiceOrderSearchFilter filter, int pageNumber, int pageSize);` e remover `GetByCustomerNameAsync`, `GetBySerialNumberAsync`, `GetByModelAsync`, `GetByDateRangeAsync`; refletir em `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderQueryRepository.cs` (remover os 4; `SearchAsync` guarda `LastSearchFilter` e `LastPagination` e devolve `SearchResponse` configurável, padrão página vazia 200)

---

## Phase 3: User Story 1 — Achar uma OS pelos filtros (P1) 🎯 MVP

**Goal**: buscar por um filtro de cada vez e ver a tabela paginada.

**Independent Test**: quickstart §2 itens 1–7.

### Backend

- [X] T005 [P] [US1] Criar `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrdersQueries/ServiceOrderSearchQuery.cs`: `public record ServiceOrderSearchQuery(long? Number, string? Customer, string? Model, string? SerialNumber, DateTime? From, DateTime? To, int PageNumber = 1, int PageSize = 20);`
- [X] T006 [US1] Escrever testes em `src/TVSApp.Tests/Handlers/ServiceOrderQueryHandlerTests.cs` para `Search` (regras de data-model.md): nenhum filtro → 400 "Preencha um filtro por vez."; dois filtros (ex.: cliente + modelo; número + período) → 400 igual; `Number = 0` → 400 "Informe um número de OS válido."; cliente "ma" (ou "  ma  ") → 400 "Digite pelo menos 3 letras do cliente."; `To < From` → 400 "A data final não pode ser antes da inicial."; `PageNumber = 0` ou `PageSize = 0`/`101` → 400 "Paginação inválida."; textos chegam ao repositório com trim (" lg " → "lg"); só `From` → filtro com `To = hoje`; só `To` → `From = null`; datas com hora chegam sem hora; caso válido → 200 com a página do repositório e mensagem "{n} ordens encontradas." (ou "Nenhuma ordem de serviço encontrada." com 0); nenhuma regra chama o repositório quando dá 400
- [X] T007 [US1] Implementar `Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> Search(ServiceOrderSearchQuery query)` em `src/TVSApp.Application/Handlers/IServiceOrderQueryHandler.cs` e `ServiceOrderQueryHandler.cs` conforme T006 (conta filtros: número, cliente, modelo, série — não vazios após trim — e período se `From` ou `To`); T006 deve passar
- [X] T008 [US1] Implementar `SearchAsync` em `src/TVSApp.Infrastructure/Repositories/ServiceOrders/ServiceOrderQueryRepository.cs`: `AsNoTracking`; filtro único: `Id == Number` | `EF.Functions.Like(Customer.Name.CustomerName, %termo%)` | idem `Product.Model` | idem `Product.SerialNumber` | `EntryDate >= From` (se houver) e `EntryDate < To.AddDays(1)` (se houver); `OrderByDescending(Id)`; `Select` para `ServiceOrderSearchItem` (`IsSale = Product.Defect == "VENDA"`); `CountAsync` + `Skip/Take`; `PaginatedResult` com `totalPages` como nas abas. Remover `GetByCustomerNameAsync`, `GetBySerialNumberAsync`, `GetByModelAsync`, `GetByDateRangeAsync`
- [X] T009 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`: adicionar `GET /service-orders/search` (`IServiceOrderQueryHandler`, `[AsParameters] ServiceOrderSearchQuery query` → `handler.Search(query)` → `ToResult()`, `.WithTags("ServiceOrder").RequireAuthorization()`) **antes** de `/service-orders/{id:long}`; remover `/get-service-orders-by-customer-name`, `/get-service-orders-by-serial-number`, `/get-service-orders-by-model`, `/get-service-orders-by-date`; apagar `GetServiceOrdersBy{CustomerName,SerialNumber,Model,Date}Query.cs` em `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrdersQueries/`

### Frontend

- [X] T010 [P] [US1] Criar `web/src/app/features/search-service-orders/models/service-order-search.model.ts` (`ServiceOrderSearchItem` com os campos do contrato; `ServiceOrderSearchParams { number?, customer?, model?, serialNumber?, from?, to?, pageNumber, pageSize }`) e `search-service-orders.service.ts` com `search(params): Observable<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>>` (`GET ${apiUrl}/service-orders/search`, só os parâmetros preenchidos; datas `yyyy-MM-dd`) e `getById(id): Observable<BaseResponse<ServiceOrder>>` (`GET ${apiUrl}/service-orders/{id}`)
- [X] T011 [US1] Criar `web/src/app/features/search-service-orders/search-service-orders.component.{ts,html,scss}` (`app-search-service-orders`, título "Buscar OS"): barra de filtros no topo (Nº OS `type="number"`, Cliente, Modelo, Número de Série, De e Até com `mat-datepicker`), botões **Limpar** e **Buscar** no fim da barra (quebra em duas linhas em telas estreitas); tabela `mat-table` com colunas OS, Cliente, Aparelho (tipo + marca + modelo), Série, Empresa, Situação ("Venda" se `isSale`, senão label da situação), Entrada (`dd/MM/yyyy`); `mat-paginator` de 20 com o total ("{n} encontradas"); estado em signals (filtros, página, resultados, carregando); Buscar volta para a página 1; trocar de página repete a última busca; Enter em qualquer campo busca. Visual igual às tabelas da Home (cores do `_brand`); SCSS < 4 kB
- [X] T012 [US1] "Um filtro por vez" em `search-service-orders.component.ts`: ao mudar um campo (`valueChanges`), limpar os outros com `{ emitEvent: false }`; De e Até contam como um grupo (mudar um não limpa o outro)
- [X] T013 [US1] Adicionar `{ path: 'buscar', loadComponent: () => import('./features/search-service-orders/search-service-orders.component').then(m => m.SearchServiceOrdersComponent), canActivate: [authGuard] }` em `web/src/app/app.routes.ts` e `route: '/buscar'` no item "Buscar OS" de `web/src/app/shared/layout/base-layout/base-layout.component.ts`

**Checkpoint**: busca por cada filtro funcionando, com paginação.

---

## Phase 4: User Story 2 — Abrir a OS encontrada (P1)

**Goal**: painel só de leitura ao clicar numa linha.

**Independent Test**: quickstart §2 item 8.

- [X] T014 [US2] Criar `web/src/app/features/search-service-orders/service-order-details-dialog/service-order-details-dialog.component.{ts,html,scss}`: recebe `{ id }`, carrega com `getById` (spinner; erro → mensagem dentro do diálogo); seções **Cliente** (nome, telefones mascarados, documento formatado, endereço), **Aparelho** (tipo, marca, modelo, série, defeito, acessórios, localização), **Empresa e situação** (empresa, situação/"Venda", resultado do orçamento), **Orçamento** (solução, garantia, peça, mão de obra, total em R$), **Datas** (entrada, avaliação, resposta, compra de peça, conserto, entrega; "—" se vazia); só leitura, botão **Fechar**, Esc fecha; largura ~48rem, `maxWidth: 95vw`
- [X] T015 [US2] Em `search-service-orders.component.{ts,html}`: clique na linha e Enter com a linha em foco (`tabindex="0"`, `role="button"`, `aria-label="Ver OS {n}"`) abrem o diálogo; ao fechar, filtros, resultados e página continuam

---

## Phase 5: User Story 3 — Sem resultado e erros (P2)

**Goal**: mensagens e validações na tela.

**Independent Test**: quickstart §2 itens 2, 3, 6 e 9.

- [X] T016 [US3] Em `search-service-orders.component.{ts,html}`: Buscar bloqueado sem filtro; Cliente com 1–2 letras e Até antes de De ficam em vermelho e não buscam (validadores no form, sem mat-error); lista vazia mostra "Nenhuma ordem de serviço encontrada." com os filtros mantidos; erro da API → snackbar com a mensagem (`error.error?.message` ou "Não foi possível buscar. Tente de novo."), filtros mantidos; **Limpar** apaga filtros, resultados e total

---

## Phase 6: Polish & validação

- [X] T017 [P] `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` verdes; `grep -rn "get-service-orders-by-\(customer-name\|serial-number\|model\|date\)" src web/src` vazio
- [X] T018 [P] `cd web && npx ng build` sem erro (pacote inicial < 1 MB; `search-service-orders` como chunk sob demanda; SCSS novos < 4 kB)
- [ ] T019 Roteiro manual do quickstart §2

---

## Dependencies & Execution Order

- T001 → Foundational (T002 ∥ T003 → T004) → US1.
- US1 backend: T005 → T006 → T007; T008 depende de T004; T009 depende de T007.
- US1 frontend: T010 ∥ backend; T011 → T012 → T013.
- US2 (T014 → T015) depende de T010/T011. US3 (T016) depende de T011.
- Polish por último.

### Parallel Opportunities

- T002 ∥ T003; T005 ∥ T010; T008 ∥ T006/T007 (arquivos diferentes, depois de T004); T014 ∥ T012/T013; T017 ∥ T018.

### Parallel Example: User Story 1

```text
T005 ServiceOrderSearchQuery.cs  |  T008 SearchAsync no repositório  |  T010 serviço + modelos do frontend
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 → já dá para achar OS por qualquer filtro.
2. **US2**: painel de detalhes (sem ele a busca só mostra a linha).
3. **US3**: mensagens e validações (pequeno; pode entrar junto com T011).
4. Publicar API + web juntos (as rotas antigas de busca saem).
