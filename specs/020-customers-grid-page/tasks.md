# Tasks: Tela Clientes no padrão da tela de Ordens

**Input**: Design documents from `/specs/020-customers-grid-page/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/customers-search.md, quickstart.md

**Tests**: incluídos — validações do handler (Constituição IV) e busca real no SQL Server (integração).

**Organization**: US1 ver e pesquisar clientes numa grade · US2 editar a partir da grade · US3 "Buscar OS" → "Ordens".

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests`, `dotnet test src/TVSApp.IntegrationTests` (Docker) e `cd web && npm run build`

---

## Phase 2: Foundational (layout comum)

- [X] T002 Criar `web/src/app/shared/styles/_list-page.scss` com `@mixin list-page` extraindo de `web/src/app/features/search-service-orders/search-service-orders.component.scss` as regras de layout comuns (`.page`, `.filters` com campos e `.filter-actions`, `.sheet`, `.scroll`, tabela base, cabeçalho, células, `.result-row` clicável com foco/hover, `.empty`, `.footer` com `.total` e paginador, responsivo); trocar o trecho correspondente em `search-service-orders.component.scss` por `@include list-page;` mantendo só o que é específico das ordens (larguras `.f-number`, `.f-wide`, `.f-date`, colunas). Conferir visualmente que a tela de ordens não muda (desktop 1280 px e celular 390 px)

**Checkpoint**: tela de ordens idêntica à anterior.

---

## Phase 3: User Story 1 — Ver e pesquisar clientes numa grade (P1) 🎯 MVP

**Goal**: grade de clientes com filtros Nome/Telefone/CPF-CNPJ, mais recentes primeiro, paginação 20.

**Independent Test**: abrir Clientes → grade com os mais recentes; telefone formatado → encontra; nome sem acento → encontra (quickstart §2 itens 2–6).

### Backend

- [X] T003 [P] [US1] Criar `src/TVSApp.Domain/Repositories/Customers/CustomerSearchFilter.cs` (`public record CustomerSearchFilter(string? Name, string? PhoneDigits, string? DocumentDigits);`) e adicionar `Task<BaseResponse<PaginatedResult<Customer>>> SearchAsync(CustomerSearchFilter filter, int pageNumber, int pageSize);` em `src/TVSApp.Domain/Repositories/Customers/ICustomerQueryRepository.cs`
- [X] T004 [P] [US1] Criar `src/TVSApp.Application/Requests/CustomerRequests/CustomerQueries/CustomerSearchQuery.cs` (`public record CustomerSearchQuery(string? Name, string? Phone, string? Document, int PageNumber = 1, int PageSize = 20);`)
- [X] T005 [US1] Em `src/TVSApp.Tests/Handlers/Fakes/FakeCustomerQueryRepository.cs` implementar `SearchAsync` guardando o último filtro/página (`LastSearchFilter`, `LastSearchPage`, `LastSearchPageSize`) e devolvendo `PaginatedResult` a partir da lista em memória
- [X] T006 [US1] Testes em `src/TVSApp.Tests/Handlers/CustomerHandlerTests.cs` para `Search`: sem filtros → repassa filtro vazio e página 1/20; `name` aparado e com < 3 caracteres → 400 "Digite pelo menos 3 letras do nome."; `phone` "(41) 99999-0000" → `PhoneDigits` "41999990000"; `phone` com < 4 dígitos → 400 "Digite pelo menos 4 números do telefone."; `document` "123.456.789-09" → `DocumentDigits` "12345678909"; `document` com < 3 caracteres → 400 "Digite pelo menos 3 caracteres do CPF/CNPJ."; `pageNumber` < 1 ou `pageSize` fora de 1–100 → 400 "Paginação inválida."; resultado mapeado para `CustomerDto`; mensagem "N clientes encontrados." / "Nenhum cliente encontrado."
- [X] T007 [US1] Implementar `Search(CustomerSearchQuery query)` em `src/TVSApp.Application/Handlers/Customers/CustomerQueryHandler.cs` e `ICustomerQueryHandler.cs` retornando `BaseResponse<PaginatedResult<CustomerDto>>` com as regras do T006 (filtros combinados; campos vazios/brancos = sem filtro); T006 deve passar
- [X] T008 [US1] Implementar `SearchAsync` em `src/TVSApp.Infrastructure/Repositories/Customers/CustomerQueryRepository.cs`: `AsNoTracking`; nome com `EF.Functions.Collate(c.Name.CustomerName, "Latin1_General_CI_AI").Contains(term)` (term em maiúsculas); telefone comparando só dígitos: `Phone.CustomerPhone` e `Phone2.CustomerPhone` com `.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "").Replace(".", "").Replace("+", "")` e `Contains(digits)` (principal **ou** secundário); documento `c.Document != null && c.Document.Number.Contains(term)`; `OrderByDescending(c => c.Id)`; `CountAsync` + `Skip/Take`; `PaginatedResult` com `totalPages`
- [X] T009 [US1] Adicionar em `src/TVSApp.Api/Endpoints/Customers/CustomerQueryEndpoints.cs` a rota `GET /customers/search` (`[AsParameters] CustomerSearchQuery`), `RequireAuthorization`, tag "Customer"; manter `GET /customers?name=` intacta
- [X] T010 [US1] Testes de integração em `src/TVSApp.IntegrationTests/Fluxos/ClientesTests.cs`: cria clientes com nomes únicos (um "JOÃO …" com acento), telefones gravados com e sem pontuação e um com o número no `phone2`, um com CPF; cenários "Sem filtros traz os mais recentes primeiro" (ordem decrescente de id), "Nome sem acento encontra nome com acento" (`joao` → `JOÃO`), "Telefone formatado encontra número com ou sem pontuação, principal ou secundário", "CPF com pontuação encontra o cliente", "Filtros combinados", "Paginação de 20 com total", "Nome curto é recusado" (400), "Rota antiga /customers?name= continua igual"

### Frontend

- [X] T011 [P] [US1] Criar `web/src/app/features/customers/models/customer-search.model.ts` (`CustomerSearchParams { name?, phone?, document?, pageNumber, pageSize }`) e em `web/src/app/features/customers/customers.service.ts` adicionar `searchPaged(params)` → `GET {apiUrl}/customers/search` com os parâmetros não vazios, retornando `BaseResponse<PaginatedResult<Customer>>` (reusar a interface de página já usada pela busca de OS, ou criar `shared/models/paginated-result.interface.ts` se não houver)
- [X] T012 [US1] Reescrever `web/src/app/features/customers/customers-page/customers-page.component.{ts,html,scss}` no molde de `search-service-orders`: `app-base-layout title="Clientes"`; formulário reativo (`name`, `phone`, `document`) com `mat-form-field` Nome (classe larga, placeholder "Mínimo 3 letras"), Telefone (`inputmode="tel"`), CPF/CNPJ; botões **Limpar** (outlined) e **Buscar** (filled, spinner ao carregar); `mat-table` com colunas `name` (Nome), `phone` (Telefone: `phone` e, abaixo em texto menor, `phone2` se houver), `document` (CPF/CNPJ via `formatDocument` de `models/document.ts`, ou "—"), `city` (Cidade/UF ou "—"), `email` (E-mail ou "—"); linha vazia: "Buscando…" / "Nenhum cliente encontrado." / "Nenhum cliente cadastrado." / "Carregando…"; rodapé com "Últimos clientes · N no total" (sem filtro) ou "N encontrado(s)" e `mat-paginator` (20, `hidePageSize`); carregar a primeira página ao abrir; `switchMap` para a última pesquisa vencer; erro → `SnackbarService` com a mensagem da API e mantém a grade; validação no front espelhando a API (nome ≥ 3, telefone ≥ 4 dígitos, documento ≥ 3); SCSS com `@include list-page;` e `@include stacked-table($inset: 0.75rem);`; importar `StackedTableDirective`

**Checkpoint**: grade, filtros e paginação funcionando.

---

## Phase 4: User Story 2 — Editar o cliente a partir da grade (P1)

**Goal**: clique na linha abre a edição; grade atualiza ao salvar e em tempo real.

**Independent Test**: quickstart §2 itens 7–8.

- [X] T013 [US2] Em `customers-page.component.{ts,html}`: linha `tabindex="0" role="button" [attr.aria-label]="'Editar cliente ' + row.name"` com `(click)` e `(keydown.enter)` abrindo `CustomerDialogComponent` (`{ customer, mode: 'edit' }`, `width: '44rem'`, `maxWidth: '95vw'`, como hoje); ao fechar com resultado (cliente salvo) recarregar a página atual com os mesmos filtros; cancelar não recarrega
- [X] T014 [US2] Em `customers-page.component.ts`: assinar `ServiceOrderHubService` (evento `Atualizar`, como em `features/home/home.component.ts`) e recarregar a página atual com os filtros atuais; cancelar a assinatura ao destruir

**Checkpoint**: editar pela grade e ver a atualização.

---

## Phase 5: User Story 3 — "Buscar OS" passa a se chamar "Ordens" (P2)

**Goal**: nenhum "Buscar OS" visível; links antigos funcionam.

**Independent Test**: quickstart §2 item 1.

- [X] T015 [P] [US3] Em `web/src/app/app.routes.ts`: rota `path: 'ordens'`, `title: 'Ordens'` (mesmo componente) e `{ path: 'buscar', redirectTo: 'ordens', pathMatch: 'full' }` antes do `**`
- [X] T016 [P] [US3] Em `web/src/app/shared/layout/base-layout/base-layout.component.ts` o item do menu vira `{ label: 'Ordens', icon: 'receipt_long', route: '/ordens' }`; em `web/src/app/features/search-service-orders/search-service-orders.component.html` `app-base-layout title="Ordens"`; buscar em `web/src` qualquer outro "Buscar OS" ou `'/buscar'` e ajustar

**Checkpoint**: menu, título e aba dizem "Ordens"; `/buscar` abre `/ordens`.

---

## Phase 6: Polish & validação

- [X] T017 [P] Atualizar `README.md`: rota `GET /customers/search` na seção Clientes e endereço (query, regras, respostas, exemplo) e `CustomerSearchQuery` em Modelos de dados; "Buscar OS" → "Ordens" (tela) onde aparecer; cenários novos na seção Testes (verificação)
- [X] T018 `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests`, `dotnet test src/TVSApp.IntegrationTests`, `cd web && npm run build` — todos verdes
- [X] T019 Validação visual no navegador (stack local em Docker com dados de exemplo): quickstart §2 itens 1–10 — prints de Clientes e Ordens lado a lado em 1280 px, Clientes em 390 px (cartões, sem rolagem lateral, rola até o fim), pesquisa por telefone formatado, clique abre edição

---

## Dependencies & Execution Order

- T001 → T002 (Foundational) → histórias.
- **US1 backend**: T003 ∥ T004 → T005 → T006 → T007 → T008 → T009 → T010.
- **US1 frontend**: T011 (∥ backend) → T012 (precisa de T002 e da rota T009 para testar).
- **US2**: T013 → T014 (mesmos arquivos do T012 → depois dele).
- **US3**: T015 ∥ T016 — independentes de US1/US2.
- Polish por último.

### Parallel Opportunities

- T003 ∥ T004 ∥ T011; US3 (T015 ∥ T016) em paralelo com o backend do US1; T017 ∥ T018.

### Parallel Example

```text
Backend:  T003 filtro/contrato  |  T004 query      Frontend: T011 serviço/modelo    US3: T015 rotas | T016 menu/título
```

## Implementation Strategy

1. **MVP**: layout comum + US1 (grade com filtros e paginação).
2. **US2**: edição pela grade + tempo real.
3. **US3**: renomear para "Ordens".
4. Docs e validação visual.

## Notas da implementação

- Testes: 239 unitários (21 novos de `CustomerSearchHandlerTests`) e 42 de integração (8 novos em `ClientesTests`, contra SQL Server real: nome sem acento, telefone com/sem pontuação no principal e no secundário, CPF, filtros combinados, paginação, rota antiga intacta).
- Validação no Chrome (16 verificações): menu "Ordens", `/buscar` → `/ordens`, título da aba, grade com 20 + "Últimos clientes · 25 no total", mais recente primeiro, busca por telefone sem pontuação, nome "joao" → "JOÃO", CPF com pontuação, página 2, clique abre a edição, celular sem rolagem lateral, com cartões e lista rolável.
- A pesquisa trata erros dentro de cada requisição (`catchError` no `switchMap`), para uma falha de rede não travar a tela.
- Fora do escopo, observado: o paginador do Angular Material mostra textos em inglês ("1 – 20 of 25", "Items per page") em todas as telas.
